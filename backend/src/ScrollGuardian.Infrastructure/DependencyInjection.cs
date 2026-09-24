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
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? (dbProvider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase)
                ? "Host=localhost;Database=scrollguardian;Username=postgres;Password=postgres"
                : "Data Source=scrollguardian.db");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (dbProvider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

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
