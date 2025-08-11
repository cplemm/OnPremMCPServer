using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Relay;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables() // Allow environment variables to override config file
    .Build();

// Get relay configuration
var relayConfig = configuration.GetSection(RelayConfiguration.SectionName).Get<RelayConfiguration>();
if (relayConfig == null)
{
    throw new InvalidOperationException($"Missing configuration section: {RelayConfiguration.SectionName}");
}

// Validate configuration
relayConfig.Validate();

// Create host for logging
using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.Configure<RelayConfiguration>(configuration.GetSection(RelayConfiguration.SectionName));
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// configure relay listener
var uri = new Uri($"sb://{relayConfig.Namespace}/{relayConfig.HybridConnectionPath}");
var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(relayConfig.SasKeyName, relayConfig.SasKey);
var listener = new HybridConnectionListener(uri, tokenProvider);

// Load machine state from file
var machineState = await MachineStateManager.LoadStateAsync();
// logger.LogInformation("Loaded state for {Count} machines", machineState.Count);

listener.RequestHandler = async (ctx) =>
{
    try
    {
        var req = ctx.Request;
        var res = ctx.Response;

        if (!string.Equals(req.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            res.StatusCode = HttpStatusCode.MethodNotAllowed;
            await res.CloseAsync();
            return;
        }

        var route = (req.Url?.AbsolutePath ?? string.Empty).Trim('/');
        
        // Remove the hybrid connection path from the route (if required)
        var hcPath = relayConfig.HybridConnectionPath.Trim('/');
        if (!string.IsNullOrEmpty(hcPath) && route.StartsWith(hcPath + "/", StringComparison.OrdinalIgnoreCase))
        {
            route = route.Substring(hcPath.Length + 1);
        }
        else if (!string.IsNullOrEmpty(hcPath) && route.Equals(hcPath, StringComparison.OrdinalIgnoreCase))
        {
            route = "";
        }
        var machine = req.Url != null ? GetQueryParam(req.Url, "machine") : null;
        if (string.IsNullOrWhiteSpace(route) || string.IsNullOrWhiteSpace(machine))
        {
            res.StatusCode = HttpStatusCode.BadRequest;
            await WriteJsonAsync(res, new { error = "Route or machine missing" });
            return;
        }

        logger.LogInformation("Processing request: {Route} for machine: {Machine}", route, machine);

        // main 'logic' of the on-prem server :)
        switch (route.ToLowerInvariant())
        {
            case "start":
                machineState[machine] = true;
                await MachineStateManager.SaveStateAsync(machineState);
                await WriteJsonAsync(res, new { machine, status = "started" });
                logger.LogInformation("Machine {Machine} started", machine);
                break;
            case "stop":
                machineState[machine] = false;
                await MachineStateManager.SaveStateAsync(machineState);
                await WriteJsonAsync(res, new { machine, status = "stopped" });
                logger.LogInformation("Machine {Machine} stopped", machine);
                break;
            case "get_status":
                // Refresh machine state from file to get the most current state
                var currentState = await MachineStateManager.LoadStateAsync();
                currentState.TryGetValue(machine, out var isOn);
                await WriteJsonAsync(res, new { machine, isOn });
                logger.LogDebug("Machine {Machine} status checked: {Status}", machine, isOn);
                break;
            default:
                res.StatusCode = HttpStatusCode.NotFound;
                await WriteJsonAsync(res, new { error = "Unknown route" });
                logger.LogWarning("Unknown route requested: {Route}", route);
                break;
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing request");
        ctx.Response.StatusCode = HttpStatusCode.InternalServerError;
        await WriteJsonAsync(ctx.Response, new { error = ex.Message });
    }
};

listener.Connecting += (o, e) =>
{
    logger.LogInformation("Client connecting...");
};

// Create cancellation token for graceful shutdown
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    logger.LogInformation("Shutdown requested, closing listener...");
    cts.Cancel();
};

logger.LogInformation("Starting MCP Server listener (Azure Relay Hybrid Connection)...");
logger.LogInformation("Namespace: {Namespace}, Path: {Path}", relayConfig.Namespace, relayConfig.HybridConnectionPath);
await listener.OpenAsync();
logger.LogInformation("Listening. Press Ctrl+C to exit.");

try
{
    await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);
}
catch (OperationCanceledException)
{
    logger.LogInformation("Shutdown signal received");
}

logger.LogInformation("Closing listener...");
await listener.CloseAsync();
logger.LogInformation("Server stopped");

static async Task WriteJsonAsync(RelayedHttpListenerResponse res, object obj)
{
    res.StatusCode = HttpStatusCode.OK;
    res.Headers["Content-Type"] = "application/json";
    var json = JsonSerializer.Serialize(obj);
    var buffer = Encoding.UTF8.GetBytes(json);
    await res.OutputStream.WriteAsync(buffer, 0, buffer.Length);
    await res.CloseAsync();
}

static string? GetQueryParam(Uri url, string key)
{
    var q = url.Query; // starts with ?
    if (string.IsNullOrEmpty(q)) return null;
    // very small parser: ?a=b&machine=LINE1
    foreach (var part in q.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
        var kv = part.Split('=', 2);
        if (kv.Length == 2 && string.Equals(kv[0], key, StringComparison.OrdinalIgnoreCase))
        {
            return WebUtility.UrlDecode(kv[1]);
        }
    }
    return null;
}

