using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;

// Build configuration from appsettings.json and environment variables
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var serverUrl = configuration["McpClient:ServerUrl"] ?? 
    throw new InvalidOperationException("McpClient:ServerUrl configuration is missing");

var endpoint = configuration["AzureOpenAI:Endpoint"] ?? 
    throw new InvalidOperationException("AzureOpenAI:Endpoint configuration is missing");

var apiKey = configuration["AzureOpenAI:ApiKey"] ?? 
    throw new InvalidOperationException("AzureOpenAI:ApiKey configuration is missing");

var deployment = configuration["AzureOpenAI:DeploymentName"] ?? 
    throw new InvalidOperationException("AzureOpenAI:DeploymentName configuration is missing");

Console.WriteLine("Protected MCP Client");
Console.WriteLine($"Connecting to MCP machine server at {serverUrl}...");
Console.WriteLine();

// We can customize a shared HttpClient with a custom handler if desired
var sharedHandler = new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
};
var httpClient = new HttpClient(sharedHandler);
var transport = new SseClientTransport(new()
{
    Endpoint = new Uri(serverUrl),
    Name = "MCP_Machine_Client",
}, httpClient);

var client = await McpClientFactory.CreateAsync(transport);
var tools = await client.ListToolsAsync();
if (tools.Count == 0)
{
    Console.WriteLine("No tools available on the server.");
    return;
}

Console.WriteLine($"Found {tools.Count} tools on the server.");
Console.WriteLine();

foreach (var tool in tools)
{
    Console.WriteLine($"Tool: {tool.Name} - {tool.Description}");
}

// Prepare and build kernel with the MCP tools as Kernel functions
var builder = Kernel.CreateBuilder();
builder.Services
    .AddAzureOpenAIChatCompletion(
        endpoint: endpoint,
        deploymentName: deployment,
        apiKey: apiKey);
Kernel kernel = builder.Build();
#pragma warning disable SKEXP0001 // Suppress experimental API warning for AsKernelFunction
kernel.Plugins.AddFromFunctions("MCP_Machine_Client", tools.Select(aiFunction => aiFunction.AsKernelFunction()));
#pragma warning restore SKEXP0001

// Enable automatic function calling
#pragma warning disable SKEXP0001 // Suppress experimental API warning for RetainArgumentTypes
AzureOpenAIPromptExecutionSettings executionSettings = new()
{
    Temperature = 0,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
};
#pragma warning restore SKEXP0001

// Define the agent
#pragma warning disable SKEXP0101 // Suppress experimental API warning for ChatCompletionAgent
ChatCompletionAgent agent = new()
{
    Instructions = "Answer questions about machines and performm actions on machines.",
    Name = "MachineAgent",
    Kernel = kernel,
    Arguments = new KernelArguments(executionSettings),
};

// var chat = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();
history.AddSystemMessage("You control machines. Ask for missing details like the machine name.");

Console.WriteLine("Type requests like: 'Start machine XYZ' or 'Stop machine ABC'. Ctrl+C to exit.");
while (true)
{
    Console.Write("\nUser> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;

    history.AddUserMessage(input);
    ChatMessageContent response = await agent.InvokeAsync(history).FirstAsync();

    // var result = await chat.GetChatMessageContentAsync(history, exec, kernel);
    history.Add(response);
    Console.WriteLine($"\nAssistant> {response.Content}");
}
#pragma warning restore SKEXP0101
