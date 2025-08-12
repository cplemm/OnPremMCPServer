# On-Premises MCP Server Solution

A sample solution demonstrating secure remote machine management using the Model Context Protocol (MCP) with Azure services. This solution enables natural language control of (simulated) on-premises machines without requiring inbound firewall ports, using Azure Relay for secure outbound-only connections.

## 🏗️ Architecture Overview

This solution implements a hybrid cloud architecture with the following components:
- **On-premises server**: (simulated) machine control process that runs behind a corporate firewall with outbound-only connectivity
- **Cloud components** AI-powered natural language interfaces and protocol bridging
- **Azure Relay** secure bi-directional communication without inbound ports
- **MCP (Model Context Protocol)** tool discovery and execution

![](doc/architecture.png)

A user sends a prompt (e.g. `stop machine 'foo'`) from the browser to the web client app that is hosted in Azure Container Apps:

![](doc/webclient.jpg)

The web app maintains a Semantic Kernel agent that has a connection to an Azure OpenAI LLM and also a connection to the MCP server that is hosted in another Container App. The agent processes the prompt (using the LLM) and determines if an MCP tool call is required and which tool to use (e.g. the tool to stop a machine).

If a tool call is required, the agent sends the request to the MCP server with the corresponding parameters. The MCP Server maintains a connection to an Azure Relay hybrid connection endpoint, which it can use to send requests to the on-premises server. For this to work, the on-premises server process also needs to maintain a connection to the same Azure Relay hybrid connection endpoint. The Relay will then broker the communication between the two endpoints, and the on-prem server process will get the request from the MCP server in Azure, execute the command (in this case stop the machine) and return the result.

## 📦 Solution Components

### 1. [On-Premises Server](app/server/README.md)
**Firewall-Friendly Machine Controller**

A lightweight server component designed to run on-premises behind corporate firewalls. It simulates controlling machines while maintaining secure outbound-only connections to the cloud MCP server via Azure Relay. This component handles machine state persistence (local JSON file) and processes control commands (start/stop/status) for machines. The design allows for on-demand connection establishment, making it suitable for production environments where network resources are carefully managed.

### 2. [MCP Server](app/mcpserver/README.md)
**Cloud-Hosted Protocol Bridge**

The core MCP (Model Context Protocol) server implementation that acts as a bridge between AI clients and on-premises machines. Built with ASP.NET Core, it exposes standardized MCP tools for machine management operations while securely communicating with on-premises servers through an Azure Relay Hybrid Connection. The server provides RESTful endpoints for tool discovery, handles protocol negotiation, and maintains connection state. It's containerized for easy deployment to Azure Container Apps and includes logging and health monitoring.

### 3. [Web Client](app/webclient/README.md)
**Browser-Based Chat Interface**

An ASP.NET Core web application that provides a chat interface for interacting with machines through natural language. The web client offers an intuitive Bootstrap-based UI with real-time message exchange, tool discovery panel, and example command buttons. It's containerized and ready for deployment to Azure Container Apps, making it perfect for team collaboration and remote access scenarios.

### 4. [Console Client](app/client/README.md) 
**AI-Powered Command Line Interface**

A simple console application that connects to the MCP server and provides natural language interaction with machine management tools. Built with Microsoft Semantic Kernel and Azure OpenAI, this client acts as an AI agent that can understand conversational commands like "start machine foo" or "check the status of machine 'bar'" and translate them into appropriate tool executions. Features automatic MCP tool discovery, context-aware conversations, and seamless integration with Azure OpenAI services.

## 🚀 Getting Started 
The fastest way to get started with this repo is spinning the environment up in GitHub Codespaces, as it will set up everything for you autgomatically. You can also [set it up locally](#local-environment).

### GitHub Codespaces
Open a web-based VS Code tab in your browser:

[![Open in GitHub Codespaces](https://img.shields.io/static/v1?style=for-the-badge&label=GitHub+Codespaces&message=Open&color=brightgreen&logo=github)](https://github.com/codespaces/new?template_repository=cplemm/OnPremMCPServer)

Continue with the [deployment](#deployment).

### Local Environment
1. Install the required tools:
    - [Azure Developer CLI](https://aka.ms/azure-dev/install)
    - [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Clone this repo:
```bash  
git clone https://github.com/cplemm/OnPremMCPServer.git
```

## 🌐 Deployment

### Deploy Azure Services

The steps below will provision the following major Azure resources via Bicep templates:
- Azure Container Registry for hosting container images
- Azure Container Apps Environment for running cloud components
- Azure Container Apps for the MCP Server and the Web Client
- Azure Service Bus Relay for secure hybrid connectivity
- Azure Open AI Instance for the LLM model endpoint
- Log Analytics workspace for monitoring

Enter the following commands inside a terminal in the root directory of the repo. 

1. Login to your Azure account:

    ```shell
    azd auth login
    ```

    For GitHub Codespaces users, if the previous command fails, try:

   ```shell
    azd auth login --use-device-code
    ```

2. Create a new azd environment:

    ```shell
    azd env new
    ```

    Enter a name that will be used for the resource group.
    This will create a new `.azure` folder and set it as the active environment for any calls to `azd` going forward.
   
3. Start provisioning of the Azure resources:

    ```shell
    azd provision
    ```

    You will have to select your subscription and an Azure region, and specify a name for the target resource group (rgName).

4. Wait for the provisioning process to complete.
5. Optional: you can test the app & function locally before deploying them to Azure. 
   
     - Server
       - Copy the example configuration files and update the settings with your Azure relay settings:
         - In the ./app/server folder, create a copy of the ```appsettings.example.json``` file and name it ```appsettings.json```.
         - Fill in all required configuration values => you can find them in the Azure Portal in the Azure Relay service you have provisioned above.
         - Open a terminal window and navigate to the server directory (```cd ./app/server```)
         - Start the MCP Server locally by running ```dotnet run```
         - The server will connect to the Azure Relay Hybrid Connection endpoint and listen for messages from the MCP Server.
     - MCP Server
       - Copy the example configuration files and update the settings with your Azure relay settings:
         - In the ./app/mcpserver folder, create a copy of the ```appsettings.example.json``` file and name it ```appsettings.json```.
         - Fill in all required configuration values => you can find them in the Azure Portal in the Azure Relay service you have provisioned above.
         - Open a terminal window and navigate to the mcpserver directory (```cd ./app/mcpserver```)
         - Start the MCP Server locally by running ```dotnet run --urls=http://localhost:5000```
         - The server will listen on http://localhost:5000.
         - You can use the MCP Inspector to test the server, or continue with the client or web client (see below).
     - Web Client
         - In the ./app/webclient folder, create a copy of the ```appsettings.example.json``` file and name it ```appsettings.json```.
         - Fill the required configuration values for the MCP Server (http://localhost:5000) and Azure OpenAI => again, find these values in the Azure Portal. 
         - Open a NEW terminal window and navigate to the Client directory (```cd ./app/webclient```)
         - Start the web app locally by running ```dotnet run --urls=http://localhost:5001```
         - Open the browser with http://localhost:5001 and test the app.

### Deploy Web App & MCP Server

1.  The statement below will provision (a) the Web App for the chat UI and (b) the MCP Server, both into Azure Container Apps into the same Container Environment.
   
    ```shell
    azd deploy
    ```

2. Wait for the deployment process to complete.
3. (You can also combine the provisioning & deployment steps above in a single go using ```azd up```).  
4. After deployment, you might have to manually set the container images for the Web Client and MCP Server in the Azure Portal:
   - Go to the Azure Portal and navigate to the Container Apps Environment you have created.
   - Select the Web Client app and go to the "Containers" tab.
     - Select the container image  from the registry in the resource group that has 'webclient' in its name.
     - Select the image tag that has been built by the `azd deploy` statement above.
     - Save your changes as a new revision.
   - Select the MCP Server app and go to the "Containers" tab.
     - Select the container image  from the registry in the resource group that has 'mcpserver' in its name.
     - Select the image tag that has been built by the `azd deploy` statement above.
     - Save your changes as a new revision.

## Clean up

1.  To clean up all the resources created by this sample run the following statement, which will delete all resources, incl. the resource group.

    ```shell
    azd down --purge
    ```

## 🔒 Security Features

- **Zero Inbound Ports**: On-premises components use outbound-only connections
- **Azure Relay**: Secure tunneling without VPN complexity
- **Easy Auth**: If you want to protect the web applications, consider configuring [Easy Auth](https://learn.microsoft.com/en-us/azure/container-apps/authentication) for user authentication.
- **MCP Server API**: The MCP Server is configured with Ingress that allows access only from within the Container Apps environment.

*Note*: In a production environment, the MCP Server should be secured using OAuth 2.1, as described in the [specification](https://modelcontextprotocol.io/specification/draft/basic/authorization).

## 🛠️ Technology Stack

- **.NET 9.0**: Modern, cross-platform runtime
- **Model Context Protocol (MCP)**: Standardized AI tool integration
- **Microsoft Semantic Kernel**: AI orchestration framework
- **Azure OpenAI**: Large language model services
- **Azure Service Bus Relay**: Hybrid cloud connectivity
- **ASP.NET Core**: Web framework for servers and clients
- **Docker**: Containerization for cloud deployment
- **Bicep**: Infrastructure as Code templates

## 🕸 Azure Relay Configuration & Usage
In this sample, the on-premises server and MCP Server processes each keep the connection to the Azure Relay open for the duration of their lifetime. This allows them to send and receive messages through the relay without needing to re-establish the connection for each message. As hybrid connections are [charged by the hour](https://azure.microsoft.com/en-us/pricing/details/service-bus/), it might make sense to connect to the relay 'on-demand' only, i.e. whenever commands will be sent from the cloud to the on-premises server. For example, an on-premises client app could explicitly trigger opening the connection whenever a command needs to be sent to a machine. It's probably a good idea anyway to have someone close to the machine when commands will be controlled by an LLM 😉.

In scenarios where you might have multiple on-prem servers or processes, you could multiplex those over a single connection to the Azure Relay to save costs. Check out the [Azure Relay Bridge](https://github.com/Azure/azure-relay-bridge) repo: it demonstrates this pattern in terms of a ready-to-go tool that allows creating TCP, UDP, HTTP, and Unix Socket tunnels between any pair of networked hosts that can "see" the public Internet outbound with port 443 open, allowing to traverse NATs and Firewalls without requiring VPNs. The bridge runs on Windows, Linux, and MacOS and you can mix and match different OSses on the "local" (client) and "remote" (server) end. 

## 🤝 Contributing

This is a demonstration solution. For production use, consider implementing additional security measures, error handling, logging, and monitoring based on your organizational requirements.