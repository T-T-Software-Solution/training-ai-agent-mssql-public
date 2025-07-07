using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using AgentAI.Application;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AgentAI.Infrastructure;

public class AuthenticationInfraService : IAuthenticationInfraService
{
    private readonly JwtOptions _jwtOptions;
    private readonly AuthOptions _authOptions;
    private readonly ILogger<AuthenticationInfraService> _logger;
    public AuthenticationInfraService(
        ILogger<AuthenticationInfraService> logger,
        IOptions<JwtOptions> jwtOptions,
        IOptions<AuthOptions> authSettings)
    {
        _logger = logger;
        _jwtOptions = jwtOptions.Value;
        _authOptions = authSettings.Value;
    }

    public AuthenticationModels.Login GetAdminLogin()
    {
        if (string.IsNullOrEmpty(_authOptions.AdminUser) || string.IsNullOrEmpty(_authOptions.AdminPassword))
        {
            _logger.LogError("Admin credentials are not configured.");
            throw new InvalidOperationException("Admin credentials are not configured.");
        }

        return new AuthenticationModels.Login
        {
            Username = _authOptions.AdminUser,
            Password = _authOptions.AdminPassword
        };
    }

    public string GenerateJwtToken(AuthenticationModels.Login loginModel)
    {
        if (string.IsNullOrEmpty(loginModel.Username) || string.IsNullOrEmpty(loginModel.Password))
        {
            _logger.LogError("Username or password is empty.");
            throw new ArgumentException("Username and password must not be empty.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: new[] { new Claim(ClaimTypes.Name, loginModel.Username) },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
