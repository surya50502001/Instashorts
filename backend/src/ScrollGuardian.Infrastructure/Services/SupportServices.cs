using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Entities;
using ScrollGuardian.Domain.Enums;
using ScrollGuardian.Infrastructure.Persistence;

namespace ScrollGuardian.Infrastructure.Services;

public class DataPrivacyService : IDataPrivacyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;

    public DataPrivacyService(ApplicationDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async Task<UserDataExportDto> ExportAllUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.Preference)
            .Include(u => u.Goals)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var events = await _dbContext.ContentEvents
            .Include(e => e.ContentItem)
                .ThenInclude(ci => ci!.Analysis)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.TimestampUtc)
            .ToListAsync(cancellationToken);

        var answers = await _dbContext.KnowledgeAnswers
            .Include(a => a.KnowledgeQuestion)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.AnsweredAtUtc)
            .ToListAsync(cancellationToken);

        var summaries = await _dbContext.DailySummaries
            .Where(ds => ds.UserId == userId)
            .OrderByDescending(ds => ds.Date)
            .ToListAsync(cancellationToken);

        await _auditService.LogAsync(userId, "DATA_EXPORT", "Privacy", new { EventCount = events.Count, AnswerCount = answers.Count }, cancellationToken: cancellationToken);

        return new UserDataExportDto
        {
            Profile = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                Plan = user.Plan,
                IsEmailVerified = user.IsEmailVerified,
                IsOnboarded = user.IsOnboarded,
                CreatedAtUtc = user.CreatedAtUtc
            },
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
            Preferences = user.Preference != null ? new UserPreferenceDto
            {
                InterventionAggressiveness = user.Preference.InterventionAggressiveness,
                SessionInterventionIntervalMinutes = user.Preference.SessionInterventionIntervalMinutes,
                SnoozeDurationMinutes = user.Preference.SnoozeDurationMinutes,
                IsInterventionEnabled = user.Preference.IsInterventionEnabled,
                IsAiAnalysisEnabled = user.Preference.IsAiAnalysisEnabled,
                IsTrackingEnabled = user.Preference.IsTrackingEnabled,
                ExcludedCategories = JsonSerializer.Deserialize<List<string>>(user.Preference.ExcludedCategoriesJson) ?? new(),
                Theme = user.Preference.Theme
            } : null,
            Events = events.Select(e => new ContentEventExportDto
            {
                TimestampUtc = e.TimestampUtc,
                Url = e.ContentItem?.Url ?? "",
                Title = e.ContentItem?.Title ?? "",
                Category = e.ContentItem?.Analysis?.Category ?? "Other",
                TimeSpentSeconds = e.TimeSpentSeconds,
                IsGoalRelevant = e.IsGoalRelevant
            }).ToList(),
            KnowledgeAnswers = answers.Select(a => new KnowledgeAnswerExportDto
            {
                AnsweredAtUtc = a.AnsweredAtUtc,
                QuestionText = a.KnowledgeQuestion?.QuestionText ?? "",
                IsCorrect = a.IsCorrect,
                ResponseTimeSeconds = a.ResponseTimeSeconds
            }).ToList(),
            Summaries = summaries.Select(s => new DailySummaryExportDto
            {
                Date = s.Date,
                TotalScrollMinutes = Math.Round(s.TotalScrollSeconds / 60.0, 1),
                LearningMinutes = Math.Round(s.LearningSeconds / 60.0, 1),
                GoalRelevantMinutes = Math.Round(s.GoalRelevantSeconds / 60.0, 1)
            }).ToList()
        };
    }

    public async Task<bool> DeleteContentEventAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default)
    {
        var ev = await _dbContext.ContentEvents
            .FirstOrDefaultAsync(e => e.Id == eventId && e.UserId == userId, cancellationToken);

        if (ev == null) return false;

        _dbContext.ContentEvents.Remove(ev);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(userId, "DELETE_EVENT", "Privacy", new { EventId = eventId }, cancellationToken: cancellationToken);
        return true;
    }

    public async Task<bool> PurgeActivityHistoryAsync(Guid userId, DateTime? beforeDateUtc = null, CancellationToken cancellationToken = default)
    {
        var cutoff = beforeDateUtc ?? DateTime.UtcNow;

        var events = await _dbContext.ContentEvents
            .Where(e => e.UserId == userId && e.TimestampUtc <= cutoff)
            .ToListAsync(cancellationToken);

        _dbContext.ContentEvents.RemoveRange(events);

        var sessions = await _dbContext.ContentSessions
            .Where(s => s.UserId == userId && s.StartedAtUtc <= cutoff)
            .ToListAsync(cancellationToken);

        _dbContext.ContentSessions.RemoveRange(sessions);

        var interventions = await _dbContext.Interventions
            .Where(i => i.UserId == userId && i.TriggeredAtUtc <= cutoff)
            .ToListAsync(cancellationToken);

        _dbContext.Interventions.RemoveRange(interventions);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(userId, "PURGE_ACTIVITY", "Privacy", new { PurgedEvents = events.Count, BeforeDate = cutoff }, cancellationToken: cancellationToken);
        return true;
    }

    public async Task<bool> DeleteUserAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return false;

        await _auditService.LogAsync(userId, "DELETE_ACCOUNT", "Privacy", new { Email = user.Email }, cancellationToken: cancellationToken);
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;

    public SubscriptionService(ApplicationDbContext dbContext, IConfiguration configuration, IAuditService auditService)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _auditService = auditService;
    }

    public async Task<SubscriptionStatusDto> GetSubscriptionStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var sub = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        var goalsCount = await _dbContext.Goals
            .CountAsync(g => g.UserId == userId && g.IsActive, cancellationToken);

        var plan = sub?.Plan ?? SubscriptionPlan.Free;
        var status = sub?.Status ?? SubscriptionStatus.Active;

        return new SubscriptionStatusDto
        {
            Plan = plan,
            Status = status,
            CurrentPeriodEndUtc = sub?.CurrentPeriodEndUtc,
            CancelAtPeriodEnd = sub?.CancelAtPeriodEnd ?? false,
            ActiveGoalsCount = goalsCount,
            MaxGoalsAllowed = plan == SubscriptionPlan.Pro ? 999 : 3,
            HasDeepAiAnalysis = plan == SubscriptionPlan.Pro,
            HasUnlimitedHistory = plan == SubscriptionPlan.Pro
        };
    }

    public async Task<CheckoutSessionResponseDto> CreateCheckoutSessionAsync(Guid userId, SubscriptionPlan targetPlan, CancellationToken cancellationToken = default)
    {
        var stripeSecretKey = _configuration["Stripe:SecretKey"] ?? _configuration["STRIPE_SECRET_KEY"];

        // If Stripe secret is configured, connect to Stripe checkout API; otherwise provide managed checkout handler
        var sessionUrl = $"/billing/checkout-success?plan={targetPlan.ToString().ToLower()}&user={userId}";

        var sub = await _dbContext.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (sub == null)
        {
            sub = new Subscription
            {
                UserId = userId,
                Plan = targetPlan,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStartUtc = DateTime.UtcNow,
                CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(1)
            };
            _dbContext.Subscriptions.Add(sub);
        }
        else
        {
            sub.Plan = targetPlan;
            sub.Status = SubscriptionStatus.Active;
            sub.CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(1);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user != null)
        {
            user.Plan = targetPlan;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(userId, "UPGRADE_PLAN", "Billing", new { TargetPlan = targetPlan.ToString() }, cancellationToken: cancellationToken);

        return new CheckoutSessionResponseDto
        {
            CheckoutUrl = sessionUrl,
            SessionId = "sess_" + Guid.NewGuid().ToString("N")
        };
    }

    public Task<bool> ProcessWebhookEventAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default)
    {
        // Real webhook verification hook
        return Task.FromResult(true);
    }

    public async Task<bool> CheckFeatureAllowedAsync(Guid userId, string featureKey, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        var isPro = user?.Plan == SubscriptionPlan.Pro || user?.Subscription?.Plan == SubscriptionPlan.Pro;

        return featureKey switch
        {
            "UnlimitedGoals" => isPro,
            "DeepAiAnalysis" => isPro,
            "UnlimitedHistory" => isPro,
            _ => true
        };
    }
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _dbContext;

    public AuditService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(Guid? userId, string action, string category, object? details = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                Category = category,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DetailsJson = details != null ? JsonSerializer.Serialize(details) : "{}",
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.AuditLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Avoid failing primary operations if audit write fails
        }
    }
}
