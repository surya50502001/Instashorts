using Microsoft.EntityFrameworkCore;
using ScrollGuardian.Domain.Entities;

namespace ScrollGuardian.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserGoal> Goals { get; }
    DbSet<UserPreference> Preferences { get; }
    DbSet<PrivacyConsent> PrivacyConsents { get; }
    DbSet<ContentItem> ContentItems { get; }
    DbSet<ContentAnalysis> ContentAnalyses { get; }
    DbSet<ContentSession> ContentSessions { get; }
    DbSet<ContentEvent> ContentEvents { get; }
    DbSet<ContentCategory> ContentCategories { get; }
    DbSet<ContentTopic> ContentTopics { get; }
    DbSet<UserTopic> UserTopics { get; }
    DbSet<KnowledgeCheck> KnowledgeChecks { get; }
    DbSet<KnowledgeQuestion> KnowledgeQuestions { get; }
    DbSet<KnowledgeAnswer> KnowledgeAnswers { get; }
    DbSet<LearningProgress> LearningProgresses { get; }
    DbSet<Intervention> Interventions { get; }
    DbSet<Recommendation> Recommendations { get; }
    DbSet<RecommendationInteraction> RecommendationInteractions { get; }
    DbSet<DailySummary> DailySummaries { get; }
    DbSet<WeeklySummary> WeeklySummaries { get; }
    DbSet<Device> Devices { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
