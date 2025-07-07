using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentAI.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SoftwareOptions>(configuration.GetSection("Software"));
        services.Configure<LineOptions>(configuration.GetSection("Line"));
        services.Configure<AzureOpenAIOptions>(configuration.GetSection("AzureOpenAI"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<AuthOptions>(configuration.GetSection("Auth"));
        services.Configure<SqlServerOptions>(configuration.GetSection("SqlServer"));
        services.Configure<AzureAdOptions>(configuration.GetSection("AzureAd"));
        services.Configure<MCPOptions>(configuration.GetSection("MCP"));
        return services;
    }

    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<ISoftwareBusinessService, SoftwareBusinessService>();
        services.AddScoped<IMessagingBusinessService, MessagingBusinessService>();
        services.AddScoped<IErrorLogBusinessService, ErrorLogBusinessService>();
        services.AddScoped<IUserBusinessService, UserBusinessService>();
        services.AddScoped<IAuthenticationBusinessService, AuthenticationBusinessService>();
        return services;
    }
}
