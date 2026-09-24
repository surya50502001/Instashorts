using ScrollGuardian.Domain.Enums;

namespace ScrollGuardian.Domain.Entities;

public class ContentItem : BaseEntity
{
    public string ContentHash { get; set; } = string.Empty; // SHA-256 hash of URL/Text for fast deduplication
    public ContentSourceProviderType SourceProvider { get; set; } = ContentSourceProviderType.BrowserExtension;
    public string PlatformContentId { get; set; } = string.Empty; // e.g. "CtgX-71..."
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string RawTranscript { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public double DurationSeconds { get; set; }
    public DateTime? AnalyzedAtUtc { get; set; }

    // Navigation
    public ContentAnalysis? Analysis { get; set; }
    public ICollection<ContentEvent> Events { get; set; } = new List<ContentEvent>();
    public ICollection<KnowledgeQuestion> KnowledgeQuestions { get; set; } = new List<KnowledgeQuestion>();
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
}

public class ContentAnalysis : BaseEntity
{
    public Guid ContentItemId { get; set; }
    public ContentItem? ContentItem { get; set; }

    public string Category { get; set; } = "Other"; // Education, Programming, Finance, Science, Fitness, Business, News, Entertainment, etc.
    public string PrimaryTopic { get; set; } = string.Empty;
    public string SecondaryTopicsJson { get; set; } = "[]"; // JSON array of strings

    // Content Quality Model dimensions (0 - 100)
    public double InformationDepth { get; set; } // 0 (surface/shallow) to 100 (deep educational/technical)
    public double EducationalValue { get; set; } // 0 (pure diversion) to 100 (high practical skill)
    public double Novelty { get; set; }          // 0 (cliché/repetitive) to 100 (novel insights)
    public double GoalRelevance { get; set; }     // calculated dynamically or baseline estimated
    public double RepetitionScore { get; set; }  // similarity to trending or recent tropes
    public double PracticalValue { get; set; }   // actionable real-world utility
    public double SourceConfidence { get; set; } // 0-100 heuristic confidence of claims

    public string Summary { get; set; } = string.Empty;
    public string KeyTakeawaysJson { get; set; } = "[]";
    public string ClaimsRequiringVerificationJson { get; set; } = "[]";

    // Observability & AI Cost Tracking
    public string AiModelUsed { get; set; } = "gemini-1.5-pro";
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public decimal CostEstimatedUsd { get; set; }
    public string RawAiResponseJson { get; set; } = "{}";
}

public class ContentSession : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid? DeviceId { get; set; }
    public Device? Device { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public double TotalDurationSeconds { get; set; }
    public double ActiveScrollDurationSeconds { get; set; }
    public double IdleDurationSeconds { get; set; }
    public int ItemCount { get; set; }
    public string DominantCategory { get; set; } = string.Empty;
    public SessionStatus Status { get; set; } = SessionStatus.Active;

    public ICollection<ContentEvent> Events { get; set; } = new List<ContentEvent>();
    public ICollection<Intervention> Interventions { get; set; } = new List<Intervention>();
}

public class ContentEvent : BaseEntity
{
    public Guid SessionId { get; set; }
    public ContentSession? Session { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid ContentItemId { get; set; }
    public ContentItem? ContentItem { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public double TimeSpentSeconds { get; set; }
    public double CompletionPercentage { get; set; }
    public ContentUserAction UserAction { get; set; } = ContentUserAction.WatchedPartial;
    public bool IsGoalRelevant { get; set; }
}

public class ContentCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultColor { get; set; } = "#3b82f6";
    public bool IsActive { get; set; } = true;
    public ICollection<ContentTopic> Topics { get; set; } = new List<ContentTopic>();
}

public class ContentTopic : BaseEntity
{
    public Guid CategoryId { get; set; }
    public ContentCategory? Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentTopicId { get; set; }
    public ContentTopic? ParentTopic { get; set; }
    public string Description { get; set; } = string.Empty;
    public string KeywordsJson { get; set; } = "[]";

    public ICollection<ContentTopic> ChildTopics { get; set; } = new List<ContentTopic>();
    public ICollection<UserTopic> UserTopics { get; set; } = new List<UserTopic>();
    public ICollection<KnowledgeQuestion> KnowledgeQuestions { get; set; } = new List<KnowledgeQuestion>();
}

public class UserTopic : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid TopicId { get; set; }
    public ContentTopic? Topic { get; set; }

    public int ContentCount { get; set; }
    public double TotalTimeSpentSeconds { get; set; }
    public int KnowledgeChecksAttempted { get; set; }
    public int KnowledgeChecksCorrect { get; set; }
    public double MasteryScore { get; set; } // 0-100 based on accuracy & exposure
    public DateTime? LastExposedAtUtc { get; set; }
}
