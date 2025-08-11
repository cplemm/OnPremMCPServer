using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace webclient.Services;

public class McpChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<McpChatService> _logger;
    private object? _mcpClient; // We'll use var when actually creating it
    private ChatCompletionAgent? _agent;
    private bool _isInitialized = false;
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);

    public McpChatService(IConfiguration configuration, ILogger<McpChatService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _initSemaphore.WaitAsync();
        try
        {
            if (_isInitialized) return;

            var serverUrl = _configuration["McpClient:ServerUrl"] ?? 
                throw new InvalidOperationException("McpClient:ServerUrl configuration is missing");

            var endpoint = _configuration["AzureOpenAI:Endpoint"] ?? 
                throw new InvalidOperationException("AzureOpenAI:Endpoint configuration is missing");

            var apiKey = _configuration["AzureOpenAI:ApiKey"] ?? 
                throw new InvalidOperationException("AzureOpenAI:ApiKey configuration is missing");

            var deployment = _configuration["AzureOpenAI:DeploymentName"] ?? 
                throw new InvalidOperationException("AzureOpenAI:DeploymentName configuration is missing");

            _logger.LogInformation("Connecting to MCP server at {ServerUrl}", serverUrl);

            // Setup HTTP client and transport - exact same as console client
            var sharedHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            };
            var httpClient = new HttpClient(sharedHandler);
            var transport = new SseClientTransport(new()
            {
                Endpoint = new Uri(serverUrl),
                Name = "MCP_Web_Client",
            }, httpClient);

            // Create MCP client exactly like console client
            var client = await McpClientFactory.CreateAsync(transport);
            _mcpClient = client; // Store it but we'll use local var for operations

            var tools = await client.ListToolsAsync();
            _logger.LogInformation("Found {ToolCount} tools on the server", tools.Count);

            // Build Semantic Kernel exactly like console client
            var builder = Kernel.CreateBuilder();
            builder.Services.AddAzureOpenAIChatCompletion(
                endpoint: endpoint,
                deploymentName: deployment,
                apiKey: apiKey);

            var kernel = builder.Build();
            
            #pragma warning disable SKEXP0001
            kernel.Plugins.AddFromFunctions("MCP_Machine_Client", tools.Select(aiFunction => aiFunction.AsKernelFunction()));
            #pragma warning restore SKEXP0001

            // Create execution settings exactly like console client
            #pragma warning disable SKEXP0001
            var executionSettings = new AzureOpenAIPromptExecutionSettings()
            {
                Temperature = 0,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
            };
            #pragma warning restore SKEXP0001

            // Create the agent exactly like console client
            #pragma warning disable SKEXP0101
            _agent = new ChatCompletionAgent()
            {
                Instructions = "Answer questions about machines and perform actions on machines. Ask for missing details like the machine name when needed.",
                Name = "MachineAgent",
                Kernel = kernel,
                Arguments = new KernelArguments(executionSettings),
            };
            #pragma warning restore SKEXP0101

            _isInitialized = true;
            _logger.LogInformation("MCP Chat Service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MCP Chat Service");
            throw;
        }
        finally
        {
            _initSemaphore.Release();
        }
    }

    public async Task<string> SendMessageAsync(string message, List<ChatMessage> chatHistory)
    {
        await InitializeAsync();

        if (_agent == null)
            throw new InvalidOperationException("Agent is not initialized");

        try
        {
            // Convert chat history to Semantic Kernel format
            var history = new ChatHistory();
            history.AddSystemMessage("You control machines. Ask for missing details like the machine name.");

            // Add previous messages from history
            foreach (var msg in chatHistory)
            {
                if (msg.IsUser)
                    history.AddUserMessage(msg.Content);
                else
                    history.AddAssistantMessage(msg.Content);
            }

            // Add the new user message
            history.AddUserMessage(message);

            // Get response from agent exactly like console client
            ChatMessageContent response = await _agent.InvokeAsync(history).FirstAsync();

            return response.Content ?? "I'm sorry, I couldn't generate a response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to agent");
            return "I'm sorry, there was an error processing your request.";
        }
    }

    public async Task<List<string>> GetAvailableToolsAsync()
    {
        if (!_isInitialized || _mcpClient == null)
        {
            // Initialize if needed and get fresh client reference
            await InitializeAsync();
        }

        try
        {
            // Create a fresh client connection like in initialization
            var serverUrl = _configuration["McpClient:ServerUrl"] ?? 
                throw new InvalidOperationException("McpClient:ServerUrl configuration is missing");

            var sharedHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            };
            var httpClient = new HttpClient(sharedHandler);
            var transport = new SseClientTransport(new()
            {
                Endpoint = new Uri(serverUrl),
                Name = "MCP_Web_Client_Tools",
            }, httpClient);

            var client = await McpClientFactory.CreateAsync(transport);
            var tools = await client.ListToolsAsync();
            
            // Convert to list of strings
            var toolsList = new List<string>();
            foreach (var tool in tools)
            {
                toolsList.Add($"{tool.Name}: {tool.Description}");
            }
            return toolsList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available tools");
            return new List<string> { "Error loading tools: " + ex.Message };
        }
    }
}

public class ChatMessage
{
    public string Content { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
