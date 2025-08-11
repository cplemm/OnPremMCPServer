public class RelayConfiguration
{
    public const string SectionName = "RelayConfiguration";
    
    public string Namespace { get; set; } = string.Empty;
    public string HybridConnectionPath { get; set; } = string.Empty;
    public string SasKeyName { get; set; } = string.Empty;
    public string SasKey { get; set; } = string.Empty;
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Namespace))
            throw new InvalidOperationException("RelayConfiguration:Namespace is required");
            
        if (string.IsNullOrWhiteSpace(HybridConnectionPath))
            throw new InvalidOperationException("RelayConfiguration:HybridConnectionPath is required");
            
        if (string.IsNullOrWhiteSpace(SasKeyName))
            throw new InvalidOperationException("RelayConfiguration:SasKeyName is required");
            
        if (string.IsNullOrWhiteSpace(SasKey))
            throw new InvalidOperationException("RelayConfiguration:SasKey is required");
    }
}
