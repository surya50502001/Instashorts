using ScrollGuardian.Domain.Enums;

namespace ScrollGuardian.Domain.Entities;

public class DailySummary : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateOnly Date { get; set; }

    public double TotalScrollSeconds { get; set; }
    public double LearningSeconds { get; set; }
    public double EntertainmentSeconds { get; set; }
    public double GoalRelevantSeconds { get; set; }
    public int SessionCount { get; set; }
    public int ItemsConsumedCount { get; set; }
    public int KnowledgeChecksCompleted { get; set; }
    public double KnowledgeChecksAccuracy { get; set; } // 0 - 100%
    public double RepetitionRatio { get; set; }        // 0 - 100%
    public string DominantCategory { get; set; } = "None";
    public string CategoryBreakdownJson { get; set; } = "{}"; // e.g. {"Programming": 1200, "Comedy": 800}
    public string TopicBreakdownJson { get; set; } = "{}";
}

public class WeeklySummary : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }

    public double TotalScrollSeconds { get; set; }
    public double GoalRelevantSeconds { get; set; }
    public double LearningSeconds { get; set; }
    public int TotalKnowledgeChecks { get; set; }
    public double AverageAccuracy { get; set; }
    public string InsightsJson { get; set; } = "[]"; // Dynamic narrative insights based strictly on real differences
    public string TopTopicsJson { get; set; } = "[]";
}

public class Device : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = "Browser"; // BrowserExtension, DesktopApp, Mobile
    public string Browser { get; set; } = string.Empty;
    public DateTime LastActiveAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsTrusted { get; set; } = true;
}

public class Subscription : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTime? CurrentPeriodStartUtc { get; set; }
    public DateTime? CurrentPeriodEndUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
}

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty; // e.g. "AUTH_LOGIN", "DATA_EXPORT", "DELETE_ACCOUNT", "GOAL_UPDATED"
    public string Category { get; set; } = "Security";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string DetailsJson { get; set; } = "{}";
}
