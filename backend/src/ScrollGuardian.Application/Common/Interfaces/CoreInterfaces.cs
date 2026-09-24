using ScrollGuardian.Domain.Entities;
using ScrollGuardian.Domain.Enums;

namespace ScrollGuardian.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress);
    bool ValidateAccessToken(string token, out Guid userId);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    bool IsAuthenticated { get; }
    string? Role { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}

public class AIAnalysisResult
{
    public string Category { get; set; } = "Other";
    public string PrimaryTopic { get; set; } = string.Empty;
    public List<string> SecondaryTopics { get; set; } = new();
    public double InformationDepth { get; set; } // 0 - 100
    public double EducationalValue { get; set; } // 0 - 100
    public double Novelty { get; set; }          // 0 - 100
    public double GoalRelevance { get; set; }     // 0 - 100
    public double RepetitionScore { get; set; }  // 0 - 100
    public double PracticalValue { get; set; }   // 0 - 100
    public double SourceConfidence { get; set; } // 0 - 100
    public string Summary { get; set; } = string.Empty;
    public List<string> KeyTakeaways { get; set; } = new();
    public List<string> ClaimsRequiringVerification { get; set; } = new();
    public List<AIKnowledgeQuestionGenerated> GeneratedQuestions { get; set; } = new();
    public string ModelUsed { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
}

public class AIKnowledgeQuestionGenerated
{
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectOptionIndex { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public KnowledgeQuestionDifficulty Difficulty { get; set; } = KnowledgeQuestionDifficulty.Medium;
}

public interface IAIContentAnalyzer
{
    string ProviderName { get; }
    Task<AIAnalysisResult> AnalyzeContentAsync(string title, string creator, string caption, string transcript, List<string> userGoalKeywords, CancellationToken cancellationToken = default);
}

public interface IContentSourceProvider
{
    ContentSourceProviderType SourceType { get; }
    bool CanHandle(string url);
    Task<ContentMetadataResult> ExtractMetadataAsync(string url, string? rawPayload = null, CancellationToken cancellationToken = default);
}

public class ContentMetadataResult
{
    public string PlatformContentId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string RawTranscript { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public double DurationSeconds { get; set; }
}

public interface IInterventionEngine
{
    Task<InterventionEvaluationResponseDto> EvaluateSessionInterventionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<bool> RecordInterventionActionAsync(Guid userId, Guid interventionId, InterventionUserResponse response, CancellationToken cancellationToken = default);
}

public interface IRecommendationEngine
{
    Task<List<RecommendationResponseDto>> GetRecommendationsAsync(Guid userId, int limit = 5, CancellationToken cancellationToken = default);
    Task<RecommendationResponseDto?> GetFindSomethingUsefulRecommendationAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> RecordInteractionAsync(Guid userId, Guid recommendationId, RecommendationInteractionType interactionType, CancellationToken cancellationToken = default);
}

public interface IKnowledgeRetentionService
{
    Task<KnowledgeCheckDetailDto?> GetPendingOrNextKnowledgeCheckAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SubmitAnswerResultDto> SubmitAnswerAsync(Guid userId, SubmitKnowledgeAnswerDto dto, CancellationToken cancellationToken = default);
    Task<List<KnowledgeMapNodeDto>> GetKnowledgeMapAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<RetentionSummaryDto> GetRetentionSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IAnalyticsAggregationService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid userId, DateOnly? date = null, CancellationToken cancellationToken = default);
    Task<DailyAnalyticsDto> GetDailyAnalyticsAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);
    Task<WeeklyReviewDto> GetWeeklyReviewAsync(Guid userId, DateOnly weekStartDate, CancellationToken cancellationToken = default);
    Task RebuildDailySummaryAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);
}

public interface ISubscriptionService
{
    Task<SubscriptionStatusDto> GetSubscriptionStatusAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CheckoutSessionResponseDto> CreateCheckoutSessionAsync(Guid userId, SubscriptionPlan targetPlan, CancellationToken cancellationToken = default);
    Task<bool> ProcessWebhookEventAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default);
    Task<bool> CheckFeatureAllowedAsync(Guid userId, string featureKey, CancellationToken cancellationToken = default);
}

public interface IAuditService
{
    Task LogAsync(Guid? userId, string action, string category, object? details = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
}

public interface IDataPrivacyService
{
    Task<UserDataExportDto> ExportAllUserDataAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteContentEventAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> PurgeActivityHistoryAsync(Guid userId, DateTime? beforeDateUtc = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAccountAsync(Guid userId, CancellationToken cancellationToken = default);
}
