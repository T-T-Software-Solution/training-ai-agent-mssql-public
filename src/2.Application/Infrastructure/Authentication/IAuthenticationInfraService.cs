namespace AgentAI.Application;

public interface IAuthenticationInfraService
{
    AuthenticationModels.Login GetAdminLogin();
    string GenerateJwtToken(AuthenticationModels.Login loginModel);
}
