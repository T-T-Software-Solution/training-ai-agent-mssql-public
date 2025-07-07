using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AgentAI.Application;
using AgentAI.Domain;

namespace AgentAI.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfraServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind SqlServerOptions for IOptions<SqlServerOptions> usage elsewhere
        services.Configure<SqlServerOptions>(configuration.GetSection("SqlServer"));

        // Retrieve SqlServerOptions from configuration
        var sqlServerOptions = configuration.GetSection("SqlServer").Get<SqlServerOptions>();

        // Retrieve the connection string from SqlServerOptions
        var connectionString = sqlServerOptions?.Connection;

        // Register SQL Server database context if a connection string is provided
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Register repositories
            services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IChatHistoryRepository, ChatHistoryRepository>();
            services.AddScoped<IUserReplyTokenRepository, UserReplyTokenRepository>();
            services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        }

        // Register other infra services
        services.AddScoped<IChatCompletionInfraService, ChatCompletionInfraService>();
        services.AddScoped<ILineMessagingInfraService, LineMessagingInfraService>();
        services.AddScoped<IAuthenticationInfraService, AuthenticationInfraService>();

        return services;
    }
}
