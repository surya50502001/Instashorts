using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Entities;
using ScrollGuardian.Domain.Enums;
using ScrollGuardian.Infrastructure.Persistence;

namespace ScrollGuardian.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OnboardingController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<OnboardingRequestDto> _validator;
    private readonly IAuditService _auditService;

    public OnboardingController(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IValidator<OnboardingRequestDto> validator,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _validator = validator;
        _auditService = auditService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<OnboardingStatusResponseDto>> GetStatus()
    {
        var userId = _currentUserService.UserId!.Value;
        var user = await _dbContext.Users
            .Include(u => u.Preference)
            .Include(u => u.Goals)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        return Ok(new OnboardingStatusResponseDto
        {
            IsOnboarded = user.IsOnboarded,
            Goals = user.Goals.Select(g => new UserGoalDto
            {
                Id = g.Id,
                Category = g.Category,
                Title = g.Title,
                Description = g.Description,
                Priority = g.Priority,
                TargetWeeklyHours = g.TargetWeeklyHours,
                IsActive = g.IsActive,
                CreatedAtUtc = g.CreatedAtUtc
            }).ToList(),
            Preference = user.Preference != null ? new UserPreferenceDto
            {
                InterventionAggressiveness = user.Preference.InterventionAggressiveness,
                SessionInterventionIntervalMinutes = user.Preference.SessionInterventionIntervalMinutes,
                SnoozeDurationMinutes = user.Preference.SnoozeDurationMinutes,
                IsInterventionEnabled = user.Preference.IsInterventionEnabled,
                IsAiAnalysisEnabled = user.Preference.IsAiAnalysisEnabled,
                IsTrackingEnabled = user.Preference.IsTrackingEnabled,
                ExcludedCategories = JsonSerializer.Deserialize<List<string>>(user.Preference.ExcludedCategoriesJson) ?? new(),
                Theme = user.Preference.Theme
            } : null
        });
    }

    [HttpPost]
    public async Task<ActionResult<OnboardingStatusResponseDto>> SubmitOnboarding([FromBody] OnboardingRequestDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var userId = _currentUserService.UserId!.Value;
        var user = await _dbContext.Users
            .Include(u => u.Preference)
            .Include(u => u.PrivacyConsent)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        // 1. Create Goals from Onboarding
        int priority = 1;
        foreach (var cat in dto.SelectedCategories)
        {
            var goal = new UserGoal
            {
                UserId = userId,
                Category = cat,
                Title = priority == 1 ? dto.PrimaryGoalText : $"Improve understanding in {cat}",
                Description = priority == 1 ? dto.PrimaryGoalText : $"Curated high-signal content in {cat}",
                Priority = priority++,
                TargetWeeklyHours = Math.Round((dto.EstimatedDailyMinutes * 7.0) / 60.0 * 0.5, 1),
                IsActive = true
            };
            _dbContext.Goals.Add(goal);
        }

        // 2. Set Preferences
        int intervalMinutes = dto.Aggressiveness switch
        {
            AggressivenessLevel.Gentle => 45,
            AggressivenessLevel.Balanced => 30,
            AggressivenessLevel.Proactive => 15,
            _ => 30
        };

        if (user.Preference == null)
        {
            user.Preference = new UserPreference
            {
                UserId = userId,
                InterventionAggressiveness = dto.Aggressiveness,
                SessionInterventionIntervalMinutes = intervalMinutes,
                IsInterventionEnabled = true,
                IsAiAnalysisEnabled = dto.ConsentToAiAnalysis,
                IsTrackingEnabled = dto.ConsentToTelemetry
            };
            _dbContext.Preferences.Add(user.Preference);
        }
        else
        {
            user.Preference.InterventionAggressiveness = dto.Aggressiveness;
            user.Preference.SessionInterventionIntervalMinutes = intervalMinutes;
            user.Preference.IsAiAnalysisEnabled = dto.ConsentToAiAnalysis;
            user.Preference.IsTrackingEnabled = dto.ConsentToTelemetry;
        }

        // 3. Set Privacy Consent
        if (user.PrivacyConsent == null)
        {
            user.PrivacyConsent = new PrivacyConsent
            {
                UserId = userId,
                TelemetryConsent = dto.ConsentToTelemetry,
                LocalProcessingConsent = true,
                ServerProcessingConsent = true,
                AiProcessingConsent = dto.ConsentToAiAnalysis,
                ConsentedAtUtc = DateTime.UtcNow
            };
            _dbContext.PrivacyConsents.Add(user.PrivacyConsent);
        }
        else
        {
            user.PrivacyConsent.TelemetryConsent = dto.ConsentToTelemetry;
            user.PrivacyConsent.AiProcessingConsent = dto.ConsentToAiAnalysis;
            user.PrivacyConsent.ConsentedAtUtc = DateTime.UtcNow;
        }

        user.IsOnboarded = true;
        await _dbContext.SaveChangesAsync();
        await _auditService.LogAsync(userId, "ONBOARDING_COMPLETED", "User");

        return await GetStatus();
    }
}

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GoalsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateUserGoalDto> _createValidator;

    public GoalsController(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IValidator<CreateUserGoalDto> createValidator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserGoalDto>>> GetGoals()
    {
        var userId = _currentUserService.UserId!.Value;
        var goals = await _dbContext.Goals
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Priority)
            .Select(g => new UserGoalDto
            {
                Id = g.Id,
                Category = g.Category,
                Title = g.Title,
                Description = g.Description,
                Priority = g.Priority,
                TargetWeeklyHours = g.TargetWeeklyHours,
                IsActive = g.IsActive,
                CreatedAtUtc = g.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(goals);
    }

    [HttpPost]
    public async Task<ActionResult<UserGoalDto>> CreateGoal([FromBody] CreateUserGoalDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        var userId = _currentUserService.UserId!.Value;
        var user = await _dbContext.Users.Include(u => u.Subscription).FirstOrDefaultAsync(u => u.Id == userId);
        var isPro = user?.Plan == SubscriptionPlan.Pro || user?.Subscription?.Plan == SubscriptionPlan.Pro;

        var existingCount = await _dbContext.Goals.CountAsync(g => g.UserId == userId && g.IsActive);
        if (!isPro && existingCount >= 3)
        {
            return BadRequest(new { message = "Free tier is limited to 3 active goals. Upgrade to Pro for unlimited goals." });
        }

        var goal = new UserGoal
        {
            UserId = userId,
            Category = dto.Category,
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            TargetWeeklyHours = dto.TargetWeeklyHours,
            IsActive = true
        };

        _dbContext.Goals.Add(goal);
        await _dbContext.SaveChangesAsync();

        return Ok(new UserGoalDto
        {
            Id = goal.Id,
            Category = goal.Category,
            Title = goal.Title,
            Description = goal.Description,
            Priority = goal.Priority,
            TargetWeeklyHours = goal.TargetWeeklyHours,
            IsActive = goal.IsActive,
            CreatedAtUtc = goal.CreatedAtUtc
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserGoalDto>> UpdateGoal(Guid id, [FromBody] UpdateUserGoalDto dto)
    {
        var userId = _currentUserService.UserId!.Value;
        var goal = await _dbContext.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (goal == null) return NotFound();

        goal.Title = dto.Title;
        goal.Description = dto.Description;
        goal.Priority = dto.Priority;
        goal.TargetWeeklyHours = dto.TargetWeeklyHours;
        goal.IsActive = dto.IsActive;

        await _dbContext.SaveChangesAsync();

        return Ok(new UserGoalDto
        {
            Id = goal.Id,
            Category = goal.Category,
            Title = goal.Title,
            Description = goal.Description,
            Priority = goal.Priority,
            TargetWeeklyHours = goal.TargetWeeklyHours,
            IsActive = goal.IsActive,
            CreatedAtUtc = goal.CreatedAtUtc
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGoal(Guid id)
    {
        var userId = _currentUserService.UserId!.Value;
        var goal = await _dbContext.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (goal == null) return NotFound();

        _dbContext.Goals.Remove(goal);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PreferencesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public PreferencesController(ApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<UserPreferenceDto>> GetPreferences()
    {
        var userId = _currentUserService.UserId!.Value;
        var pref = await _dbContext.Preferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (pref == null)
        {
            pref = new UserPreference { UserId = userId };
            _dbContext.Preferences.Add(pref);
            await _dbContext.SaveChangesAsync();
        }

        return Ok(new UserPreferenceDto
        {
            InterventionAggressiveness = pref.InterventionAggressiveness,
            SessionInterventionIntervalMinutes = pref.SessionInterventionIntervalMinutes,
            SnoozeDurationMinutes = pref.SnoozeDurationMinutes,
            IsInterventionEnabled = pref.IsInterventionEnabled,
            IsAiAnalysisEnabled = pref.IsAiAnalysisEnabled,
            IsTrackingEnabled = pref.IsTrackingEnabled,
            ExcludedCategories = JsonSerializer.Deserialize<List<string>>(pref.ExcludedCategoriesJson) ?? new(),
            Theme = pref.Theme
        });
    }

    [HttpPut]
    public async Task<ActionResult<UserPreferenceDto>> UpdatePreferences([FromBody] UpdateUserPreferenceDto dto)
    {
        var userId = _currentUserService.UserId!.Value;
        var pref = await _dbContext.Preferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (pref == null)
        {
            pref = new UserPreference { UserId = userId };
            _dbContext.Preferences.Add(pref);
        }

        if (dto.InterventionAggressiveness.HasValue) pref.InterventionAggressiveness = dto.InterventionAggressiveness.Value;
        if (dto.SessionInterventionIntervalMinutes.HasValue) pref.SessionInterventionIntervalMinutes = dto.SessionInterventionIntervalMinutes.Value;
        if (dto.SnoozeDurationMinutes.HasValue) pref.SnoozeDurationMinutes = dto.SnoozeDurationMinutes.Value;
        if (dto.IsInterventionEnabled.HasValue) pref.IsInterventionEnabled = dto.IsInterventionEnabled.Value;
        if (dto.IsAiAnalysisEnabled.HasValue) pref.IsAiAnalysisEnabled = dto.IsAiAnalysisEnabled.Value;
        if (dto.IsTrackingEnabled.HasValue) pref.IsTrackingEnabled = dto.IsTrackingEnabled.Value;
        if (dto.ExcludedCategories != null) pref.ExcludedCategoriesJson = JsonSerializer.Serialize(dto.ExcludedCategories);
        if (!string.IsNullOrWhiteSpace(dto.Theme)) pref.Theme = dto.Theme;

        await _dbContext.SaveChangesAsync();

        return Ok(new UserPreferenceDto
        {
            InterventionAggressiveness = pref.InterventionAggressiveness,
            SessionInterventionIntervalMinutes = pref.SessionInterventionIntervalMinutes,
            SnoozeDurationMinutes = pref.SnoozeDurationMinutes,
            IsInterventionEnabled = pref.IsInterventionEnabled,
            IsAiAnalysisEnabled = pref.IsAiAnalysisEnabled,
            IsTrackingEnabled = pref.IsTrackingEnabled,
            ExcludedCategories = JsonSerializer.Deserialize<List<string>>(pref.ExcludedCategoriesJson) ?? new(),
            Theme = pref.Theme
        });
    }
}
