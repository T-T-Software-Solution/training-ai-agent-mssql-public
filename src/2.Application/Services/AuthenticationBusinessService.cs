using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace AgentAI.Application;

public interface IAuthenticationBusinessService
{
    bool IsAdmin(AuthenticationModels.Login loginModel);
    string GenerateJwtToken(AuthenticationModels.Login loginModel);
}

public class AuthenticationBusinessService : IAuthenticationBusinessService
{
    private readonly ILogger<AuthenticationBusinessService> _logger;
    private readonly IAuthenticationInfraService _authenticationInfraService;
    public AuthenticationBusinessService(ILogger<AuthenticationBusinessService> logger, 
        IAuthenticationInfraService authenticationInfraService)
    {
        _logger = logger;
        _authenticationInfraService = authenticationInfraService;
    }

    public bool IsAdmin(AuthenticationModels.Login loginModel)
    {
        var adminLogin = _authenticationInfraService.GetAdminLogin();
        if (loginModel.Username == adminLogin.Username && loginModel.Password == adminLogin.Password)
        {
            _logger.LogInformation("Admin login successful for user: {Username}", loginModel.Username);
            return true;
        }
        
        _logger.LogWarning("Admin login failed for user: {Username}", loginModel.Username);
        return false;
    }
    
    public string GenerateJwtToken(AuthenticationModels.Login loginModel)
    {
        if (string.IsNullOrEmpty(loginModel.Username) || string.IsNullOrEmpty(loginModel.Password))
        {
            _logger.LogError("Username or password is empty.");
            throw new ArgumentException("Username and password must not be empty.");
        }
        var token = _authenticationInfraService.GenerateJwtToken(loginModel);
        _logger.LogInformation("JWT token generated successfully for user: {Username}", loginModel.Username);
        return token;
    }    
}
