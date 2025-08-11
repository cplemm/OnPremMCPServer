using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public static class MachineTool
{
    [McpServerTool, Description("Starts the machine with the name that is provided.")]
    public static async Task<string> StartMachine(string machineName)
    {
        if (string.IsNullOrWhiteSpace(machineName))
        {
            return "Invalid machine name.";
        }
        return await RelayHelper.SendRelayRequestAsync("start", machineName);
    }

    [McpServerTool, Description("Stops the machine with the name that is provided.")]
    public static async Task<string> StopMachine(string machineName)
    {
        if (string.IsNullOrWhiteSpace(machineName))
        {
            return "Invalid machine name.";
        }
        return await RelayHelper.SendRelayRequestAsync("stop", machineName);
    }

    [McpServerTool, Description("Gets the current status of the machine with the name that is provided.")]
    public static async Task<string> GetMachineStatus(string machineName)
    {
        if (string.IsNullOrWhiteSpace(machineName))
        {
            return "Invalid machine name.";
        }
        return await RelayHelper.SendRelayRequestAsync("get_status", machineName);
    }
}
