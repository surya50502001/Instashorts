namespace ScrollGuardian.Domain.Enums;

public enum UserRole
{
    User = 1,
    Admin = 2
}

public enum SubscriptionPlan
{
    Free = 1,
    Pro = 2,
    Enterprise = 3
}

public enum SubscriptionStatus
{
    Inactive = 0,
    Active = 1,
    Trialing = 2,
    PastDue = 3,
    Canceled = 4
}

public enum AggressivenessLevel
{
    Gentle = 1,    // e.g. 45m threshold, softer nudges
    Balanced = 2,  // e.g. 30m threshold, balanced interventions
    Proactive = 3  // e.g. 15m threshold, active learning opportunities
}

public enum ContentUserAction
{
    ScrolledPast = 1,
    WatchedPartial = 2,
    WatchedFull = 3,
    Liked = 4,
    Saved = 5,
    Shared = 6,
    Paused = 7
}

public enum SessionStatus
{
    Active = 1,
    Paused = 2,
    Ended = 3
}

public enum InterventionTriggerReason
{
    ProlongedPassiveScroll = 1,
    LowEducationalRatio = 2,
    HighRepetition = 3,
    GoalDrift = 4,
    ManualTrigger = 5
}

public enum InterventionSuggestedAction
{
    FindUseful = 1,
    TakeChallenge = 2,
    TakeBreak = 3,
    ContinueScrolling = 4
}

public enum InterventionUserResponse
{
    Pending = 0,
    Dismissed = 1,
    Snoozed = 2,
    AcceptedFindUseful = 3,
    AcceptedChallenge = 4,
    Ignored = 5
}

public enum KnowledgeCheckStatus
{
    Pending = 1,
    Completed = 2,
    Expired = 3,
    Skipped = 4
}

public enum KnowledgeQuestionDifficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}

public enum RecommendationInteractionType
{
    Viewed = 1,
    Clicked = 2,
    Saved = 3,
    Dismissed = 4,
    Completed = 5
}

public enum ContentSourceProviderType
{
    BrowserExtension = 1,
    YouTubeShorts = 2,
    InstagramReels = 3,
    TikTok = 4,
    ManualShare = 5,
    Import = 6
}
