using ScrollGuardian.Domain.Enums;

namespace ScrollGuardian.Application.Common.Interfaces;

// Auth DTOs
public class RegisterRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? DeviceIdentifier { get; set; }
    public string? DeviceType { get; set; }
}

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public UserProfileDto User { get; set; } = new();
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public SubscriptionPlan Plan { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsOnboarded { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

// Onboarding DTOs
public class OnboardingRequestDto
{
    public List<string> SelectedCategories { get; set; } = new(); // e.g. ["Programming", "Career", "Finance"]
    public string PrimaryGoalText { get; set; } = string.Empty;  // e.g. "I want to master .NET 8 and prepare for senior engineer interviews"
    public double EstimatedDailyMinutes { get; set; } = 45.0;
    public AggressivenessLevel Aggressiveness { get; set; } = AggressivenessLevel.Balanced;
    public bool ConsentToTelemetry { get; set; } = true;
    public bool ConsentToAiAnalysis { get; set; } = true;
}

public class OnboardingStatusResponseDto
{
    public bool IsOnboarded { get; set; }
    public List<UserGoalDto> Goals { get; set; } = new();
    public UserPreferenceDto? Preference { get; set; }
}

// Goal and Preference DTOs
public class CreateUserGoalDto
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
    public double TargetWeeklyHours { get; set; } = 3.5;
}

public class UpdateUserGoalDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
    public double TargetWeeklyHours { get; set; } = 3.5;
    public bool IsActive { get; set; } = true;
}

public class UserGoalDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public double TargetWeeklyHours { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class UserPreferenceDto
{
    public AggressivenessLevel InterventionAggressiveness { get; set; }
    public int SessionInterventionIntervalMinutes { get; set; }
    public int SnoozeDurationMinutes { get; set; }
    public bool IsInterventionEnabled { get; set; }
    public bool IsAiAnalysisEnabled { get; set; }
    public bool IsTrackingEnabled { get; set; }
    public List<string> ExcludedCategories { get; set; } = new();
    public string Theme { get; set; } = "dark";
}

public class UpdateUserPreferenceDto
{
    public AggressivenessLevel? InterventionAggressiveness { get; set; }
    public int? SessionInterventionIntervalMinutes { get; set; }
    public int? SnoozeDurationMinutes { get; set; }
    public bool? IsInterventionEnabled { get; set; }
    public bool? IsAiAnalysisEnabled { get; set; }
    public bool? IsTrackingEnabled { get; set; }
    public List<string>? ExcludedCategories { get; set; }
    public string? Theme { get; set; }
}
