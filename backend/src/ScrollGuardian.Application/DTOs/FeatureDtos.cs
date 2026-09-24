using ScrollGuardian.Domain.Enums;

namespace ScrollGuardian.Application.Common.Interfaces;

// Content DTOs
public class IngestContentEventDto
{
    public string Url { get; set; } = string.Empty;
    public string? PlatformContentId { get; set; }
    public ContentSourceProviderType SourceProvider { get; set; } = ContentSourceProviderType.BrowserExtension;
    public string? Title { get; set; }
    public string? Creator { get; set; }
    public string? Caption { get; set; }
    public string? RawTranscript { get; set; }
    public double DurationSeconds { get; set; }
    public double TimeSpentSeconds { get; set; }
    public double CompletionPercentage { get; set; }
    public ContentUserAction UserAction { get; set; } = ContentUserAction.WatchedPartial;
    public Guid? SessionId { get; set; }
    public string? DeviceIdentifier { get; set; }
}

public class ContentIngestionResponseDto
{
    public Guid EventId { get; set; }
    public Guid SessionId { get; set; }
    public Guid ContentItemId { get; set; }
    public bool AnalysisQueued { get; set; }
    public ContentAnalysisSummaryDto? ImmediateAnalysis { get; set; }
}

public class ContentAnalysisSummaryDto
{
    public string Category { get; set; } = "Other";
    public string PrimaryTopic { get; set; } = string.Empty;
    public double InformationDepth { get; set; }
    public double EducationalValue { get; set; }
    public double GoalRelevance { get; set; }
    public double RepetitionScore { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> KeyTakeaways { get; set; } = new();
}

public class ContentItemDetailDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string PlatformContentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
    public ContentSourceProviderType SourceProvider { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ContentAnalysisSummaryDto? Analysis { get; set; }
}

public class SessionHeartbeatDto
{
    public Guid? SessionId { get; set; }
    public string? DeviceIdentifier { get; set; }
    public double ActiveScrollSecondsIncrement { get; set; }
    public double IdleSecondsIncrement { get; set; }
}

public class SessionHeartbeatResponseDto
{
    public Guid SessionId { get; set; }
    public double TotalDurationSeconds { get; set; }
    public double ActiveScrollDurationSeconds { get; set; }
    public bool TriggerIntervention { get; set; }
    public InterventionEvaluationResponseDto? Intervention { get; set; }
}

// Intervention DTOs
public class InterventionEvaluationResponseDto
{
    public bool ShouldIntervene { get; set; }
    public Guid? InterventionId { get; set; }
    public InterventionTriggerReason Reason { get; set; }
    public InterventionSuggestedAction SuggestedAction { get; set; }
    public int SessionMinutes { get; set; }
    public string MessageTitle { get; set; } = string.Empty;
    public string MessageBody { get; set; } = string.Empty;
    public string CallToActionText { get; set; } = "Find something useful";
    public RecommendationResponseDto? AttachedRecommendation { get; set; }
    public KnowledgeCheckDetailDto? AttachedKnowledgeCheck { get; set; }
}

public class InterventionActionDto
{
    public Guid InterventionId { get; set; }
    public InterventionUserResponse Response { get; set; }
}

// Recommendation DTOs
public class RecommendationResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ReasonDescription { get; set; } = string.Empty; // "Why this recommendation"
    public string SourceType { get; set; } = "Curated";
    public string? TopicName { get; set; }
    public string? GoalTitle { get; set; }
    public double RelevanceScore { get; set; }
    public int Priority { get; set; }
}

public class RecommendationFeedbackDto
{
    public RecommendationInteractionType InteractionType { get; set; }
}

// Knowledge Check DTOs
public class KnowledgeCheckDetailDto
{
    public Guid CheckId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string? ContentTitle { get; set; }
    public string? ContentUrl { get; set; }
    public List<KnowledgeQuestionDto> Questions { get; set; } = new();
}

public class KnowledgeQuestionDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public KnowledgeQuestionDifficulty Difficulty { get; set; }
}

public class SubmitKnowledgeAnswerDto
{
    public Guid CheckId { get; set; }
    public Guid QuestionId { get; set; }
    public int SelectedOptionIndex { get; set; }
    public double ResponseTimeSeconds { get; set; }
}

public class SubmitAnswerResultDto
{
    public bool IsCorrect { get; set; }
    public int CorrectOptionIndex { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public double? UpdatedMasteryScore { get; set; }
    public double? UpdatedRetentionScore { get; set; }
    public bool IsCheckCompleted { get; set; }
    public double? TotalCheckScorePercentage { get; set; }
}

public class KnowledgeMapNodeDto
{
    public Guid TopicId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public string? ParentTopicName { get; set; }
    public int ContentConsumedCount { get; set; }
    public double TotalTimeSpentMinutes { get; set; }
    public int KnowledgeChecksAttempted { get; set; }
    public int KnowledgeChecksCorrect { get; set; }
    public double MasteryScore { get; set; }
    public double RetentionScore { get; set; }
    public DateTime? LastExposedAtUtc { get; set; }
}

public class RetentionSummaryDto
{
    public double OverallRetentionRate { get; set; }
    public int TotalQuestionsAnswered { get; set; }
    public int TotalQuestionsCorrect { get; set; }
    public int MasteredTopicsCount { get; set; }
    public int DevelopingTopicsCount { get; set; }
    public string RetentionStatusMessage { get; set; } = string.Empty;
}

// Analytics & Dashboard DTOs
public class DashboardSummaryDto
{
    public DateOnly Date { get; set; }
    public double ActiveScrollMinutes { get; set; }
    public double LearningMinutes { get; set; }
    public double EntertainmentMinutes { get; set; }
    public double GoalRelevantMinutes { get; set; }
    public int TotalSessions { get; set; }
    public int ItemsConsumedCount { get; set; }
    public string DominantCategory { get; set; } = "None";

    // "What's changing?" comparisons with previous period
    public double? ScrollMinutesChangePct { get; set; }
    public double? LearningMinutesChangePct { get; set; }
    public double? GoalRelevanceChangePct { get; set; }
    public string TrendComparisonText { get; set; } = string.Empty;

    // Timeline items for today
    public List<ConsumptionTimelineEventDto> Timeline { get; set; } = new();

    // Learning snippet
    public int KnowledgeChecksToday { get; set; }
    public double? KnowledgeAccuracyToday { get; set; }

    // Top recommended next step
    public RecommendationResponseDto? TopRecommendation { get; set; }
}

public class ConsumptionTimelineEventDto
{
    public Guid EventId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public string PrimaryTopic { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
    public double TimeSpentSeconds { get; set; }
    public double EducationalValue { get; set; }
    public bool IsGoalRelevant { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class DailyAnalyticsDto
{
    public DateOnly Date { get; set; }
    public double TotalScrollMinutes { get; set; }
    public double LearningMinutes { get; set; }
    public double EntertainmentMinutes { get; set; }
    public double GoalRelevantMinutes { get; set; }
    public int SessionCount { get; set; }
    public double AverageSessionMinutes { get; set; }
    public double LongestSessionMinutes { get; set; }
    public double RepetitionScore { get; set; }
    public Dictionary<string, double> CategoryDistributionMinutes { get; set; } = new();
    public Dictionary<string, double> TopicDistributionMinutes { get; set; } = new();
    public int KnowledgeChecksCompleted { get; set; }
    public double KnowledgeAccuracyRate { get; set; }
}

public class WeeklyReviewDto
{
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public double TotalScrollHours { get; set; }
    public double GoalRelevantHours { get; set; }
    public double LearningHours { get; set; }
    public int TotalKnowledgeChecks { get; set; }
    public double AverageAccuracy { get; set; }
    public List<string> DynamicInsights { get; set; } = new();
    public List<TopicSummaryDto> TopTopics { get; set; } = new();
    public List<DailyBreakdownDto> DailyBreakdown { get; set; } = new();
}

public class TopicSummaryDto
{
    public string TopicName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double MinutesSpent { get; set; }
    public int ItemsCount { get; set; }
}

public class DailyBreakdownDto
{
    public DateOnly Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public double ScrollMinutes { get; set; }
    public double LearningMinutes { get; set; }
    public double GoalRelevantMinutes { get; set; }
}

// Privacy DTOs
public class UserDataExportDto
{
    public UserProfileDto Profile { get; set; } = new();
    public List<UserGoalDto> Goals { get; set; } = new();
    public UserPreferenceDto? Preferences { get; set; }
    public List<ContentEventExportDto> Events { get; set; } = new();
    public List<KnowledgeAnswerExportDto> KnowledgeAnswers { get; set; } = new();
    public List<DailySummaryExportDto> Summaries { get; set; } = new();
    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ContentEventExportDto
{
    public DateTime TimestampUtc { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double TimeSpentSeconds { get; set; }
    public bool IsGoalRelevant { get; set; }
}

public class KnowledgeAnswerExportDto
{
    public DateTime AnsweredAtUtc { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public double ResponseTimeSeconds { get; set; }
}

public class DailySummaryExportDto
{
    public DateOnly Date { get; set; }
    public double TotalScrollMinutes { get; set; }
    public double LearningMinutes { get; set; }
    public double GoalRelevantMinutes { get; set; }
}

public class PurgeHistoryRequestDto
{
    public DateTime? BeforeDateUtc { get; set; }
}

// Subscription DTOs
public class SubscriptionStatusDto
{
    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime? CurrentPeriodEndUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public int ActiveGoalsCount { get; set; }
    public int MaxGoalsAllowed { get; set; }
    public bool HasDeepAiAnalysis { get; set; }
    public bool HasUnlimitedHistory { get; set; }
}

public class CheckoutSessionResponseDto
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}
