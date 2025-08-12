using Microsoft.Azure.Relay;
using Microsoft.Extensions.Configuration;
using System.Net;

/// <summary>
/// Static helper class that provides Azure Relay functionality for MCP server tools.
/// Tools that need relay functionality can call the static methods provided by this class.
/// </summary>
public static class RelayHelper
{
    private static readonly Lazy<RelayConnection> _relayConnection = new(() => InitializeRelayConnection());
    
    /// <summary>
    /// Gets the initialized relay connection configuration.
    /// </summary>
    private static RelayConnection Connection => _relayConnection.Value;
    
    /// <summary>
    /// Sends an HTTP POST request through the Azure Relay.
    /// </summary>
    /// <param name="route">The route to append to the relay URL</param>
    /// <param name="machine">The machine name parameter</param>
    /// <returns>The response payload from the relay</returns>
    public static async Task<string> SendRelayRequestAsync(string route, string machine)
    {
        try
        {
            var connection = Connection;
            var token = await connection.TokenProvider.GetTokenAsync(connection.RelayUri.ToString(), TimeSpan.FromHours(1));
            
            using var httpClient = new HttpClient();
            var requestUri = $"https://{connection.RelayBase.Host}/{connection.RelayBase.AbsolutePath.TrimStart('/')}/{route}?machine={WebUtility.UrlEncode(machine)}";
            
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("ServiceBusAuthorization", token.TokenString);
            
            using var response = await httpClient.SendAsync(request);
            var payload = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Relay error: {(int)response.StatusCode} {response.StatusCode} - {payload}");
            }
            return payload;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send request to relay: {ex.Message}", ex);
        }
    }
    
    private static RelayConnection InitializeRelayConnection()
    {
        // Build configuration the same way as in Program.cs
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var relayConfig = configuration.GetSection(RelayConfiguration.SectionName).Get<RelayConfiguration>();
        if (relayConfig == null)
        {
            throw new InvalidOperationException($"Missing configuration section: {RelayConfiguration.SectionName}");
        }

        relayConfig.Validate();
        var relayBase = new Uri($"https://{relayConfig.Namespace}/{relayConfig.HybridConnectionPath}".Replace("sb://", "https://"));
        var relayUri = new Uri($"sb://{relayBase.Host}/{relayBase.AbsolutePath.TrimStart('/')}");
        var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(relayConfig.SasKeyName, relayConfig.SasKey);
        
        return new RelayConnection(relayBase, relayUri, tokenProvider);
    }
}

/// <summary>
/// Holds the Azure Relay connection configuration.
/// </summary>
internal record RelayConnection(Uri RelayBase, Uri RelayUri, TokenProvider TokenProvider);
