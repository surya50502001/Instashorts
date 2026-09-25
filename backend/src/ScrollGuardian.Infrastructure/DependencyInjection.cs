using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Infrastructure.AI;
using ScrollGuardian.Infrastructure.BackgroundJobs;
using ScrollGuardian.Infrastructure.Persistence;
using ScrollGuardian.Infrastructure.Providers;
using ScrollGuardian.Infrastructure.Security;
using ScrollGuardian.Infrastructure.Services;

namespace ScrollGuardian.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dbProvider = configuration["DatabaseProvider"] ?? "Sqlite";
        var rawConnStr = configuration.GetConnectionString("DefaultConnection") 
            ?? configuration["DATABASE_URL"] 
            ?? configuration["ConnectionStrings:DefaultConnection"];

        bool isPostgres = dbProvider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase) ||
                          dbProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
                          (!string.IsNullOrWhiteSpace(rawConnStr) && 
                           (rawConnStr.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || 
                            rawConnStr.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) || 
                            rawConnStr.Contains("Host=", StringComparison.OrdinalIgnoreCase)));

        // If raw connection string is default SQLite ("Data Source="), do not treat as Postgres
        if (isPostgres && !string.IsNullOrWhiteSpace(rawConnStr) && rawConnStr.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            isPostgres = false;
        }

        if (isPostgres)
        {
            string npgsqlConnStr;
            if (string.IsNullOrWhiteSpace(rawConnStr))
            {
                npgsqlConnStr = "Host=localhost;Database=scrollguardian;Username=postgres;Password=postgres";
            }
            else if (rawConnStr.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || 
                     rawConnStr.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(rawConnStr);
                    var userInfo = uri.UserInfo.Split(':');
                    var builder = new Npgsql.NpgsqlConnectionStringBuilder
                    {
                        Host = uri.Host,
                        Port = uri.Port > 0 ? uri.Port : 5432,
                        Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "",
                        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
                        Database = uri.AbsolutePath.TrimStart('/'),
                        SslMode = Npgsql.SslMode.Prefer
                    };
                    npgsqlConnStr = builder.ConnectionString;
                }
                catch
                {
                    npgsqlConnStr = rawConnStr;
                }
            }
            else
            {
                npgsqlConnStr = rawConnStr;
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(npgsqlConnStr);
            });
        }
        else
        {
            var sqliteConnStr = (!string.IsNullOrWhiteSpace(rawConnStr) && rawConnStr.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                ? rawConnStr
                : "Data Source=scrollguardian.db";

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(sqliteConnStr);
            });
        }

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Security
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpContextAccessor();

        // AI Analyzers & Providers
        services.AddHttpClient<GeminiContentAnalyzer>();
        services.AddSingleton<RuleBasedContentAnalyzer>();
        services.AddScoped<IAIContentAnalyzer>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var geminiKey = config["AI:GeminiApiKey"] ?? config["GEMINI_API_KEY"];
            if (!string.IsNullOrWhiteSpace(geminiKey))
            {
                return provider.GetRequiredService<GeminiContentAnalyzer>();
            }
            return provider.GetRequiredService<RuleBasedContentAnalyzer>();
        });

        // Content Providers
        services.AddScoped<IContentSourceProvider, BrowserExtensionContentProvider>();
        services.AddScoped<IContentSourceProvider, YouTubeShortsProvider>();
        services.AddScoped<IContentSourceProvider, InstagramReelsProvider>();
        services.AddScoped<ContentSourceProviderFactory>();

        // Domain & Application Services
        services.AddScoped<IInterventionEngine, InterventionEngine>();
        services.AddScoped<IRecommendationEngine, RecommendationEngine>();
        services.AddScoped<IKnowledgeRetentionService, KnowledgeRetentionService>();
        services.AddScoped<IAnalyticsAggregationService, AnalyticsAggregationService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IDataPrivacyService, DataPrivacyService>();
        services.AddScoped<IAuditService, AuditService>();

        // Background Ingestion Channel & Hosted Worker
        services.AddSingleton<ContentProcessingChannel>();
        services.AddHostedService<ContentProcessingWorker>();

        return services;
    }
}
