using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AgentAI.Infrastructure;

// This factory is used by EF Core tools to create a DbContext instance at design time.
// It is typically used for migrations and scaffolding.
// The factory reads the connection string from appsettings.Development.json.
// Ensure that the appsettings.Development.json file is present in the output directory.
// This is useful for development scenarios where you want to use a different database configuration than production.
// The connection string should be defined under the "SqlServer" section in the appsettings.Development.json file. 

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Build config from appsettings.Development.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetSection("SqlServer").GetValue<string>("Connection");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
