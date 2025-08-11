# Server Configuration

This is the server process that is simulating a component sitting on-premises (behind a firewall) and is controlling a machine. It uses Azure Relay to communicate with the MCP Server in the cloud via an outbound connection. Note that in this sample, the connection will be held open by the server process as long as it is running. In a production environment, where activities would most likely be triggered by someone close to the machine, this connection should be opened on-demand. 

## Setup

1. Copy `appsettings.example.json` to `appsettings.json`
2. Update the configuration values in `appsettings.json` with your Azure Relay settings

## Configuration

The server reads configuration from `appsettings.json`. The following settings are required:

### RelayConfiguration

- **Namespace**: Your Azure Service Bus Relay namespace (e.g., `myns.servicebus.windows.net`)
- **HybridConnectionPath**: The hybrid connection path (e.g., `mcp-hc`)
- **SasKeyName**: The Shared Access Signature key name (e.g., `RootManageSharedAccessKey`)
- **SasKey**: The Shared Access Signature key value

### Example Configuration

```json
{
  "RelayConfiguration": {
    "Namespace": "<your-namespace>.servicebus.windows.net",
    "HybridConnectionPath": "mcp-hc",
    "SasKeyName": "RootManageSharedAccessKey",
    "SasKey": "<your-sas-key>"
  }
}
```

## Environment Variable Override

You can still override configuration values using environment variables. The configuration system will check environment variables after loading from the JSON file. Use the following format:

- `RelayConfiguration__Namespace`
- `RelayConfiguration__HybridConnectionPath`  
- `RelayConfiguration__SasKeyName`
- `RelayConfiguration__SasKey`

## Running the Server

```bash
dotnet run
```

The server will start and listen for connections on the configured Azure Relay hybrid connection.
