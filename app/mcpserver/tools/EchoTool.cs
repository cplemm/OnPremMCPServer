using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "No message provided.";
        }
        return $"Echo: {message}";
    }
}
