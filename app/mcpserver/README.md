# MCP Server

A Model Context Protocol (MCP) server implementation built with ASP.NET Core that provides machine management tools for remote machines through an Azure Relay Hybrid Connection. This server enables remote machine operations without requiring inbound firewall ports for on-prem environments.

This MCP server provides the following capabilities:
- **Machine Management**: Start, stop, and check status of remote machines
- **Echo Tool**: Simple echo functionality for testing
- **Azure Relay Integration**: Secure communication through Azure Service Bus Relay
- **HTTP Transport**: Standard MCP HTTP transport protocol
- **Containerized deployment** - ready for Azure Container Apps with Docker support

## Architecture

The server consists of:
- **Program.cs**: Main application entry point with MCP server configuration
- **Tools**: MCP tool implementations (`EchoTool`, `MachineTool`)
- **Relay**: Azure Relay integration (`RelayHelper`, `RelayConfiguration`)

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

The server will start and listen for MCP requests over HTTP transport.

### Production Mode

```bash
dotnet build -c Release
dotnet run -c Release
```

### Using Docker

Build and run the container (in the mcpserver directory) locally:

```bash
docker build -t mcpserver .
docker run -p 8080:8080 \
  -e RelayConfiguration__Namespace=your-namespace.servicebus.windows.net \
  -e RelayConfiguration__HybridConnectionPath=mcp-hc \
  -e RelayConfiguration__SasKeyName=RootManageSharedAccessKey \
  -e RelayConfiguration__SasKey=your_sas_key \
  mcpserver
```

To prepare the image for production, you can use the following command to publish the image to Azure Container Registry:

```bash
docker tag mcpserver <your_registry>.azurecr.io/mcpserver:v0.9 .
az acr login --name <your_registry>
docker push <your_registry>.azurecr.io/mcpserver:v0.9
```

## Available Tools

### Machine Tools

#### Start Machine
- **Description**: Starts a machine with the specified name
- **Parameters**: `machineName` (string) - Name of the machine to start
- **Usage**: Sends a "start" command through Azure Relay

#### Stop Machine
- **Description**: Stops a machine with the specified name
- **Parameters**: `machineName` (string) - Name of the machine to stop
- **Usage**: Sends a "stop" command through Azure Relay

#### Get Machine Status
- **Description**: Gets the current status of a machine
- **Parameters**: `machineName` (string) - Name of the machine to query
- **Usage**: Sends a "get_status" command through Azure Relay

### Echo Tool

#### Echo
- **Description**: Echoes the provided message back to the client
- **Parameters**: `message` (string) - Message to echo back
- **Usage**: Simple tool for testing MCP connectivity

## Development

### Adding New Tools

1. Create a new static class in the `tools` folder
2. Mark the class with `[McpServerToolType]`
3. Add static methods marked with `[McpServerTool]` and `[Description]`
4. Tools are automatically discovered at process startup and registered



