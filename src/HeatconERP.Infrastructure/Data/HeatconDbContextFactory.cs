using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HeatconERP.Infrastructure.Data;

/// <summary>
/// Used by EF Core design-time tools (migrations, etc.).
/// Loads DATABASE_URL from .env or appsettings.
/// </summary>
public class HeatconDbContextFactory : IDesignTimeDbContextFactory<HeatconDbContext>
{
    public HeatconDbContext CreateDbContext(string[] args)
    {
        // Find solution root: traverse up from Infrastructure assembly until we find .env or src/
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var dir = Path.GetFullPath(assemblyDir);
        for (var i = 0; i < 8 && dir != null; i++)
        {
            var envPath = Path.Combine(dir, ".env");
            if (File.Exists(envPath))
            {
                try { DotNetEnv.Env.Load(envPath); break; }
                catch { /* ignore */ }
            }
            dir = Path.GetDirectoryName(dir);
        }

        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrEmpty(connectionString))
        {
            var apiSettings = Path.Combine(assemblyDir, "..", "..", "..", "..", "..", "HeatconERP.API", "appsettings.json");
            apiSettings = Path.GetFullPath(apiSettings);
            if (!File.Exists(apiSettings))
                throw new FileNotFoundException($"appsettings.json not found. Set DATABASE_URL in .env at project root. Searched: {apiSettings}");
            var apiDir = Path.GetDirectoryName(apiSettings)!;
            var config = new ConfigurationBuilder()
                .SetBasePath(apiDir)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();
            connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DATABASE_URL or ConnectionStrings:DefaultConnection not found.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<HeatconDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new HeatconDbContext(optionsBuilder.Options);
    }
}
