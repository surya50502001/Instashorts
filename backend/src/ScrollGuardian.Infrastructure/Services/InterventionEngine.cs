using Microsoft.EntityFrameworkCore;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Entities;
using ScrollGuardian.Domain.Enums;
using ScrollGuardian.Infrastructure.Persistence;

namespace ScrollGuardian.Infrastructure.Services;

public class InterventionEngine : IInterventionEngine
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRecommendationEngine _recommendationEngine;
    private readonly IKnowledgeRetentionService _knowledgeRetentionService;

    public InterventionEngine(
        ApplicationDbContext dbContext,
        IRecommendationEngine recommendationEngine,
        IKnowledgeRetentionService knowledgeRetentionService)
    {
        _dbContext = dbContext;
        _recommendationEngine = recommendationEngine;
        _knowledgeRetentionService = knowledgeRetentionService;
    }

    public async Task<InterventionEvaluationResponseDto> EvaluateSessionInterventionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var preference = await _dbContext.Preferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? new UserPreference { UserId = userId };

        if (!preference.IsInterventionEnabled)
        {
            return new InterventionEvaluationResponseDto { ShouldIntervene = false };
        }

        var session = await _dbContext.ContentSessions
            .Include(s => s.Events)
                .ThenInclude(e => e.ContentItem)
                    .ThenInclude(ci => ci!.Analysis)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

        if (session == null)
        {
            return new InterventionEvaluationResponseDto { ShouldIntervene = false };
        }

        // Determine aggressiveness threshold
        int thresholdMinutes = preference.InterventionAggressiveness switch
        {
            AggressivenessLevel.Gentle => Math.Max(45, preference.SessionInterventionIntervalMinutes),
            AggressivenessLevel.Balanced => Math.Max(30, preference.SessionInterventionIntervalMinutes),
            AggressivenessLevel.Proactive => Math.Max(15, preference.SessionInterventionIntervalMinutes),
            _ => 30
        };

        var sessionMinutes = (int)Math.Max(1, (DateTime.UtcNow - session.StartedAtUtc).TotalMinutes);

        // Check if snooze is currently active
        var lastIntervention = await _dbContext.Interventions
            .Where(i => i.UserId == userId && i.SessionId == sessionId)
            .OrderByDescending(i => i.TriggeredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastIntervention != null)
        {
            if (lastIntervention.UserResponse == InterventionUserResponse.Snoozed &&
                lastIntervention.RespondedAtUtc.HasValue &&
                (DateTime.UtcNow - lastIntervention.RespondedAtUtc.Value).TotalMinutes < preference.SnoozeDurationMinutes)
            {
                return new InterventionEvaluationResponseDto { ShouldIntervene = false };
            }

            // Avoid rapid spamming if triggered in last interval minutes
            if ((DateTime.UtcNow - lastIntervention.TriggeredAtUtc).TotalMinutes < thresholdMinutes)
            {
                return new InterventionEvaluationResponseDto { ShouldIntervene = false };
            }
        }

        // Evaluate if threshold reached
        if (sessionMinutes < thresholdMinutes)
        {
            return new InterventionEvaluationResponseDto { ShouldIntervene = false, SessionMinutes = sessionMinutes };
        }

        // Analyze recent session content distribution
        var totalEvents = session.Events.Count;
        var entertainmentCount = session.Events.Count(e => e.ContentItem?.Analysis?.Category is "Entertainment" or "Comedy" or "Gaming" or "Other");
        var entertainmentRatio = totalEvents > 0 ? (double)entertainmentCount / totalEvents : 1.0;

        var reason = entertainmentRatio >= 0.6
            ? InterventionTriggerReason.LowEducationalRatio
            : InterventionTriggerReason.ProlongedPassiveScroll;

        var title = $"{sessionMinutes} minutes into this session";
        var body = entertainmentRatio >= 0.6
            ? "Most of your recent content has been entertainment. Want to make the next 10 minutes useful?"
            : "You have been scrolling continuously. Ready to shift focus toward one of your learning goals?";

        // Fetch top recommendation or quiz
        var recommendation = await _recommendationEngine.GetFindSomethingUsefulRecommendationAsync(userId, cancellationToken);
        var pendingQuiz = await _knowledgeRetentionService.GetPendingOrNextKnowledgeCheckAsync(userId, cancellationToken);

        var intervention = new Intervention
        {
            UserId = userId,
            SessionId = sessionId,
            TriggeredAtUtc = DateTime.UtcNow,
            TriggerReason = reason,
            SuggestedAction = pendingQuiz != null ? InterventionSuggestedAction.TakeChallenge : InterventionSuggestedAction.FindUseful,
            UserResponse = InterventionUserResponse.Pending,
            SessionMinutesAtTrigger = sessionMinutes,
            TriggerContextMessage = $"{title} — {body}"
        };

        _dbContext.Interventions.Add(intervention);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new InterventionEvaluationResponseDto
        {
            ShouldIntervene = true,
            InterventionId = intervention.Id,
            Reason = reason,
            SuggestedAction = intervention.SuggestedAction,
            SessionMinutes = sessionMinutes,
            MessageTitle = title,
            MessageBody = body,
            CallToActionText = pendingQuiz != null ? "Take a quick challenge" : "Find something useful",
            AttachedRecommendation = recommendation,
            AttachedKnowledgeCheck = pendingQuiz
        };
    }

    public async Task<bool> RecordInterventionActionAsync(Guid userId, Guid interventionId, InterventionUserResponse response, CancellationToken cancellationToken = default)
    {
        var intervention = await _dbContext.Interventions
            .FirstOrDefaultAsync(i => i.Id == interventionId && i.UserId == userId, cancellationToken);

        if (intervention == null) return false;

        intervention.UserResponse = response;
        intervention.RespondedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
