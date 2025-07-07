using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SemanticKernelPlugins;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly ITHChatCompletionService _chatCompletionService;

    public ChatController(
        ILogger<ChatController> logger,
        ITHChatCompletionService chatCompletionService)
    {
        _logger = logger;
        _chatCompletionService = chatCompletionService;
    }

    // [HttpPost("message")]
    // public async Task<IActionResult> GetChatMessage([FromBody] MessageRequest request)
    // {
    //     var answer = await _chatCompletionService.GetCompletion(
    //         prompt: request.Question,
    //         assistantPrompt: string.Empty);

    //     return Ok(new { Message = answer });
    // }

    // [HttpPost("semantickernelplugin")]
    // public async Task<IActionResult> GetSemanticKernelPluginMessage([FromBody] MessageRequest request)
    // {
    //     var answer = await _chatCompletionService.GetSemanticKernelPlugInCompletion(
    //         prompt: request.Question,
    //         assistantPrompt: string.Empty);

    //     return Ok(new { Message = answer });
    // }
}