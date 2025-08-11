# MCP Web Client

This is an ASP.NET Core web application that provides a chat interface for interacting with the MCP (Model Context Protocol) server through Semantic Kernel and Azure OpenAI.

The web client provides the following capabilities:

- **MCP integration** - connects to the MCP server to access available tools
- **Azure OpenAI integration** - uses Semantic Kernel with Azure OpenAI for natural language processing
- **Machine control** - allows users to control and monitor machines through chat commands
- **Automatic Tool Discovery**: Dynamically discovers and registers available MCP tools
- **Interactive Chat**: Continuous conversation with context retention
- **Containerized deployment** - ready for Azure Container Apps with Docker support

## Setup

1. Copy `appsettings.example.json` to `appsettings.json`
2. Update the configuration values in `appsettings.json` with your MCP Server URL and Azure OpenAI settings

## Configuration

The web client reads configuration from `appsettings.json`. The following settings are required:

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
dotnet run --urls=http://localhost:5001
```
The application will be available at http://localhost:5001

### Using Docker

Build and run the container (in the webclient directory) locally:

```bash
docker build -t webclient .
docker run -p 8081:8080 \
  -e McpClient__ServerUrl=https://your_namespace.your_region.azurecontainerapps.io/ \
  -e AzureOpenAI__Endpoint=https://your_azureopenai_endpoint \
  -e AzureOpenAI__DeploymentName=your_azureopenai_deployment \
  -e AzureOpenAI__ApiKey=your_azureopenai_api_key \
  webclient
```

To prepare the image for production, you can use the following command to publish the image to Azure Container Registry:

```bash
docker tag webclient <your_registry>.azurecr.io/webclient:v0.9
az acr login --name <your_registry>
docker push <your_registry>.azurecr.io/webclient:v0.9
```

## Usage Examples

1. **Chat Interface**: Type messages in the input field and press Enter or click Send
2. **Tool Discovery**: Click "Refresh Tools" to load available MCP tools
3. **Example Commands**: Use the example buttons to try common operations:
   - "Get status of machine foo"
   - "Start machine foo"  
   - "Stop machine foo"
   - "Echo hello world"

You can also pick one of the suggested examples from the right.