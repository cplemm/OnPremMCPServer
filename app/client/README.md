# MCP Console Client

An intelligent console client that connects to the MCP (Model Context Protocol) server and provides natural language interaction with machine management tools. Built with Microsoft Semantic Kernel and Azure OpenAI, this client acts as an AI agent that can understand natural language requests and execute appropriate machine operations.

This MCP client provides the following capabilities:
- **Natural Language Interface**: Interact with (simulated) machines using conversational commands
- **AI-Powered Agent**: Uses Azure OpenAI and Semantic Kernel for intelligent tool selection
- **MCP Protocol Support**: Connects to any MCP-compliant server via SSE transport
- **Automatic Tool Discovery**: Dynamically discovers and registers available MCP tools
- **Interactive Chat**: Continuous conversation with context retention

## Setup

1. Copy `appsettings.example.json` to `appsettings.json`
2. Update the configuration values in `appsettings.json` with your MCP Server URL and Azure OpenAI settings

## Configuration

The client reads configuration from `appsettings.json`. The following settings are required:

### McpClient
- **ServerUrl**: URL of the MCP server endpoint (e.g., `http://localhost:5000/` or `https://myapp.eastus.azurecontainerapps.io/`)

### AzureOpenAI
- **Endpoint**: Azure OpenAI service endpoint (e.g., `https://myopenai.openai.azure.com/`)
- **DeploymentName**: Name of the deployed model (e.g., `gpt-4o`)
- **ApiKey**: API key for Azure OpenAI 

### Example Configuration

```json
{
  "McpClient": {
    "ServerUrl": "https://<your_namespace>.<your_region>.azurecontainerapps.io/"
  },
  "AzureOpenAI": {
    "Endpoint": "https://<your_azureopenai_endpoint>",
    "DeploymentName": "<your_azureopenai_deployment>",
    "ApiKey": "<your_azureopenai_api_key>"
  }
}
```

## Running the Client

```bash
dotnet run
```

The client will:
1. Connect to the configured MCP server
2. Discover available tools
3. Initialize the AI agent with Semantic Kernel
4. Start an interactive chat session

### Production Mode

```bash
dotnet build -c Release
dotnet run -c Release
```

## Usage Examples

Once the client is running, you can interact with it using natural language:

```
User> Start machine foo
Assistant> I'll start the 'foo' machine for you.
[Tool execution and response]

User> What's the status of the machine?
Assistant> Which specific machine would you like me to check?

User> bar
Assistant> [Checks and returns machine status]
```