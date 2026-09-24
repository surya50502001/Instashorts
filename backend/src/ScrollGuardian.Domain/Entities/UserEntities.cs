using ScrollGuardian.Domain.Enums;

namespace ScrollGuardian.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiresAtUtc { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAtUtc { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
    public DateTime? LastLoginAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsOnboarded { get; set; }
    public DateTime? DeletionRequestedAtUtc { get; set; }

    // Navigation properties
    public UserPreference? Preference { get; set; }
    public PrivacyConsent? PrivacyConsent { get; set; }
    public Subscription? Subscription { get; set; }
    public ICollection<UserGoal> Goals { get; set; } = new List<UserGoal>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<ContentSession> Sessions { get; set; } = new List<ContentSession>();
    public ICollection<ContentEvent> Events { get; set; } = new List<ContentEvent>();
    public ICollection<UserTopic> UserTopics { get; set; } = new List<UserTopic>();
    public ICollection<KnowledgeCheck> KnowledgeChecks { get; set; } = new List<KnowledgeCheck>();
    public ICollection<LearningProgress> LearningProgresses { get; set; } = new List<LearningProgress>();
    public ICollection<Intervention> Interventions { get; set; } = new List<Intervention>();
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
    public ICollection<DailySummary> DailySummaries { get; set; } = new List<DailySummary>();
    public ICollection<WeeklySummary> WeeklySummaries { get; set; } = new List<WeeklySummary>();
    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? CreatedByIp { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}

public class UserGoal : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Category { get; set; } = string.Empty; // e.g. "Programming", "Career", "Fitness"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; } = 1; // 1 = highest
    public double TargetWeeklyHours { get; set; } = 3.5;
    public bool IsActive { get; set; } = true;
}

public class UserPreference : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public AggressivenessLevel InterventionAggressiveness { get; set; } = AggressivenessLevel.Balanced;
    public int SessionInterventionIntervalMinutes { get; set; } = 30;
    public int SnoozeDurationMinutes { get; set; } = 15;
    public bool IsInterventionEnabled { get; set; } = true;
    public bool IsAiAnalysisEnabled { get; set; } = true;
    public bool IsTrackingEnabled { get; set; } = true;
    public string ExcludedCategoriesJson { get; set; } = "[]";
    public string Theme { get; set; } = "dark";
}

public class PrivacyConsent : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public bool TelemetryConsent { get; set; } = true;
    public bool LocalProcessingConsent { get; set; } = true;
    public bool ServerProcessingConsent { get; set; } = true;
    public bool AiProcessingConsent { get; set; } = true;
    public int DataRetentionDays { get; set; } = 90;
    public DateTime ConsentedAtUtc { get; set; } = DateTime.UtcNow;
}
