using ScrollGuardian.Domain.Enums;

namespace ScrollGuardian.Domain.Entities;

public class KnowledgeCheck : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid? ContentItemId { get; set; }
    public ContentItem? ContentItem { get; set; }
    public Guid? TopicId { get; set; }
    public ContentTopic? Topic { get; set; }

    public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public KnowledgeCheckStatus Status { get; set; } = KnowledgeCheckStatus.Pending;
    public double? ScorePercentage { get; set; }

    public ICollection<KnowledgeQuestion> Questions { get; set; } = new List<KnowledgeQuestion>();
    public ICollection<KnowledgeAnswer> Answers { get; set; } = new List<KnowledgeAnswer>();
}

public class KnowledgeQuestion : BaseEntity
{
    public Guid KnowledgeCheckId { get; set; }
    public KnowledgeCheck? KnowledgeCheck { get; set; }
    public Guid? ContentItemId { get; set; }
    public ContentItem? ContentItem { get; set; }
    public Guid? TopicId { get; set; }
    public ContentTopic? Topic { get; set; }

    public string QuestionText { get; set; } = string.Empty;
    public string OptionsJson { get; set; } = "[]"; // JSON array of 4 option strings
    public int CorrectOptionIndex { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public KnowledgeQuestionDifficulty Difficulty { get; set; } = KnowledgeQuestionDifficulty.Medium;

    public ICollection<KnowledgeAnswer> Answers { get; set; } = new List<KnowledgeAnswer>();
}

public class KnowledgeAnswer : BaseEntity
{
    public Guid KnowledgeCheckId { get; set; }
    public KnowledgeCheck? KnowledgeCheck { get; set; }
    public Guid KnowledgeQuestionId { get; set; }
    public KnowledgeQuestion? KnowledgeQuestion { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public int SelectedOptionIndex { get; set; }
    public bool IsCorrect { get; set; }
    public double ResponseTimeSeconds { get; set; }
    public DateTime AnsweredAtUtc { get; set; } = DateTime.UtcNow;
}

public class LearningProgress : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid TopicId { get; set; }
    public ContentTopic? Topic { get; set; }

    public double RetentionScore { get; set; }   // 0-100 calculated from retention curve & correct quiz answers
    public double ConsistencyScore { get; set; } // 0-100 exposure regular intervals
    public double ExposureScore { get; set; }    // total time & items viewed
    public DateTime? LastCheckAtUtc { get; set; }
    public int TotalQuestionsAnswered { get; set; }
    public int TotalQuestionsCorrect { get; set; }
}

public class Intervention : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid? SessionId { get; set; }
    public ContentSession? Session { get; set; }

    public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;
    public InterventionTriggerReason TriggerReason { get; set; } = InterventionTriggerReason.ProlongedPassiveScroll;
    public InterventionSuggestedAction SuggestedAction { get; set; } = InterventionSuggestedAction.FindUseful;
    public InterventionUserResponse UserResponse { get; set; } = InterventionUserResponse.Pending;
    public int SessionMinutesAtTrigger { get; set; }
    public string TriggerContextMessage { get; set; } = string.Empty;
    public DateTime? RespondedAtUtc { get; set; }
}

public class Recommendation : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid? ContentItemId { get; set; }
    public ContentItem? ContentItem { get; set; }
    public Guid? GoalId { get; set; }
    public UserGoal? Goal { get; set; }
    public Guid? TopicId { get; set; }
    public ContentTopic? Topic { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ReasonDescription { get; set; } = string.Empty; // Transparent explanation: "Why this recommendation"
    public string SourceType { get; set; } = "Curated"; // Curated, Community, TopicDeepDive
    public double RelevanceScore { get; set; } = 90.0;
    public int Priority { get; set; } = 1;
    public DateTime? ExpiresAtUtc { get; set; }

    public ICollection<RecommendationInteraction> Interactions { get; set; } = new List<RecommendationInteraction>();
}

public class RecommendationInteraction : BaseEntity
{
    public Guid RecommendationId { get; set; }
    public Recommendation? Recommendation { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public RecommendationInteractionType InteractionType { get; set; } = RecommendationInteractionType.Viewed;
    public DateTime InteractedAtUtc { get; set; } = DateTime.UtcNow;
}
