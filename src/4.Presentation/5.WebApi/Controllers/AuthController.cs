using Microsoft.AspNetCore.Mvc;
using AgentAI.Application;

namespace AgentAI.Presentation.WebApi;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IAuthenticationBusinessService _authenticationBusinessService;
    public AuthController(
        ILogger<AuthController> logger,
        IAuthenticationBusinessService authenticationBusinessService)
    {
        _logger = logger;
        _authenticationBusinessService = authenticationBusinessService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] AuthenticationModels.Login login)
    {
        _logger.LogInformation("Login attempt from user: {Username}", login.Username);

        if (login == null || string.IsNullOrEmpty(login.Username) || string.IsNullOrEmpty(login.Password))
        {
            _logger.LogWarning("Login attempt with empty credentials.");
            return BadRequest("Username and password must not be empty.");
        }

        var isAdmin = _authenticationBusinessService.IsAdmin(login);
        if (isAdmin)
        {
            var token = _authenticationBusinessService.GenerateJwtToken(login);
            _logger.LogInformation("Login successful for user: {Username}", login.Username);
            return Ok(new { Token = token });
        }

        return Unauthorized();
    }
}
