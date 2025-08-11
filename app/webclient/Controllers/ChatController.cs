using Microsoft.AspNetCore.Mvc;
using webclient.Services;

namespace webclient.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly McpChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(McpChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            _logger.LogInformation("Received message: {Message}", request.Message);
            
            var response = await _chatService.SendMessageAsync(request.Message, request.ChatHistory);
            
            return Ok(new { Response = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            return StatusCode(500, new { Error = "Sorry, there was an error processing your request." });
        }
    }

    [HttpGet("tools")]
    public async Task<IActionResult> GetAvailableTools()
    {
        try
        {
            _logger.LogInformation("GetAvailableTools method called");
            var tools = await _chatService.GetAvailableToolsAsync();
            _logger.LogInformation("Retrieved {ToolCount} tools", tools.Count);
            
            return Ok(new { Tools = tools });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available tools");
            return StatusCode(500, new { Tools = new List<string> { "Error loading tools: " + ex.Message } });
        }
    }
}

public class SendMessageRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessage> ChatHistory { get; set; } = new();
}
