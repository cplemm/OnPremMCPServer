using System.Text.Json;

// Model for machine state persistence
public record MachineStateData(string MachineName, string State);

// Machine state manager class
public static class MachineStateManager
{
    private const string StateFileName = "machine-state.json";
    
    public static async Task<Dictionary<string, bool>> LoadStateAsync()
    {
        var machineState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        
        if (File.Exists(StateFileName))
        {
            try
            {
                var json = await File.ReadAllTextAsync(StateFileName);
                var stateData = JsonSerializer.Deserialize<List<MachineStateData>>(json);
                
                if (stateData != null)
                {
                    foreach (var item in stateData)
                    {
                        machineState[item.MachineName] = item.State.Equals("on", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading machine state: {ex.Message}");
            }
        }
        
        return machineState;
    }
    
    public static async Task SaveStateAsync(Dictionary<string, bool> machineState)
    {
        try
        {
            var stateData = machineState.Select(kvp => new MachineStateData(kvp.Key, kvp.Value ? "on" : "off")).ToList();
            var json = JsonSerializer.Serialize(stateData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(StateFileName, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving machine state: {ex.Message}");
        }
    }
}
