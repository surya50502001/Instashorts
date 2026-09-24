using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Entities;
using ScrollGuardian.Domain.Enums;
using ScrollGuardian.Infrastructure.BackgroundJobs;
using ScrollGuardian.Infrastructure.Persistence;
using ScrollGuardian.Infrastructure.Providers;

namespace ScrollGuardian.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ContentSourceProviderFactory _providerFactory;
    private readonly ContentProcessingChannel _processingChannel;
    private readonly IInterventionEngine _interventionEngine;
    private readonly IValidator<IngestContentEventDto> _validator;

    public ContentController(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ContentSourceProviderFactory providerFactory,
        ContentProcessingChannel processingChannel,
        IInterventionEngine interventionEngine,
        IValidator<IngestContentEventDto> validator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _providerFactory = providerFactory;
        _processingChannel = processingChannel;
        _interventionEngine = interventionEngine;
        _validator = validator;
    }

    [HttpPost("events")]
    public async Task<ActionResult<ContentIngestionResponseDto>> IngestEvent([FromBody] IngestContentEventDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        var userId = _currentUserService.UserId!.Value;

        // Verify tracking preference
        var pref = await _dbContext.Preferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (pref != null && !pref.IsTrackingEnabled)
        {
            return Ok(new ContentIngestionResponseDto { AnalysisQueued = false });
        }

        // 1. Resolve Content Source Provider
        var provider = _providerFactory.GetProvider(dto.Url, dto.SourceProvider);
        var metadata = await provider.ExtractMetadataAsync(dto.Url);

        var title = !string.IsNullOrWhiteSpace(dto.Title) ? dto.Title : metadata.Title;
        var creator = !string.IsNullOrWhiteSpace(dto.Creator) ? dto.Creator : metadata.Creator;
        var caption = !string.IsNullOrWhiteSpace(dto.Caption) ? dto.Caption : metadata.Caption;
        var transcript = !string.IsNullOrWhiteSpace(dto.RawTranscript) ? dto.RawTranscript : metadata.RawTranscript;
        var duration = dto.DurationSeconds > 0 ? dto.DurationSeconds : (metadata.DurationSeconds > 0 ? metadata.DurationSeconds : 30);

        // 2. Deduplicate ContentItem via SHA-256 Hash of clean URL
        var contentHash = ComputeSha256(dto.Url);
        var contentItem = await _dbContext.ContentItems
            .Include(ci => ci.Analysis)
            .FirstOrDefaultAsync(ci => ci.ContentHash == contentHash);

        bool isNewContent = false;
        if (contentItem == null)
        {
            isNewContent = true;
            contentItem = new ContentItem
            {
                ContentHash = contentHash,
                SourceProvider = dto.SourceProvider,
                PlatformContentId = !string.IsNullOrEmpty(dto.PlatformContentId) ? dto.PlatformContentId : metadata.PlatformContentId,
                Url = dto.Url,
                Title = title,
                Creator = creator,
                Caption = caption,
                RawTranscript = transcript,
                DurationSeconds = duration
            };
            _dbContext.ContentItems.Add(contentItem);
            await _dbContext.SaveChangesAsync();
        }

        // 3. Resolve or Create ContentSession
        ContentSession? session = null;
        if (dto.SessionId.HasValue)
        {
            session = await _dbContext.ContentSessions.FirstOrDefaultAsync(s => s.Id == dto.SessionId.Value && s.UserId == userId);
        }

        if (session == null)
        {
            // Find active session from last 15 minutes or create new
            var cutoff = DateTime.UtcNow.AddMinutes(-15);
            session = await _dbContext.ContentSessions
                .Where(s => s.UserId == userId && s.Status == SessionStatus.Active && s.StartedAtUtc >= cutoff)
                .OrderByDescending(s => s.StartedAtUtc)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                session = new ContentSession
                {
                    UserId = userId,
                    StartedAtUtc = DateTime.UtcNow,
                    Status = SessionStatus.Active,
                    ItemCount = 0,
                    TotalDurationSeconds = 0,
                    ActiveScrollDurationSeconds = 0
                };
                _dbContext.ContentSessions.Add(session);
                await _dbContext.SaveChangesAsync();
            }
        }

        // 4. Create ContentEvent
        var contentEvent = new ContentEvent
        {
            SessionId = session.Id,
            UserId = userId,
            ContentItemId = contentItem.Id,
            TimestampUtc = DateTime.UtcNow,
            TimeSpentSeconds = dto.TimeSpentSeconds > 0 ? dto.TimeSpentSeconds : 15,
            CompletionPercentage = dto.CompletionPercentage > 0 ? dto.CompletionPercentage : Math.Min(100, (dto.TimeSpentSeconds / duration) * 100),
            UserAction = dto.UserAction,
            IsGoalRelevant = false
        };

        _dbContext.ContentEvents.Add(contentEvent);

        // Update session metrics
        session.ItemCount++;
        session.TotalDurationSeconds += contentEvent.TimeSpentSeconds;
        session.ActiveScrollDurationSeconds += contentEvent.TimeSpentSeconds;

        await _dbContext.SaveChangesAsync();

        // 5. Queue for Background AI Analysis if not yet analyzed
        bool queued = false;
        if (contentItem.Analysis == null)
        {
            await _processingChannel.QueueItemAsync(new ContentProcessingQueueItem
            {
                EventId = contentEvent.Id,
                UserId = userId,
                ContentItemId = contentItem.Id
            });
            queued = true;
        }

        ContentAnalysisSummaryDto? summaryDto = null;
        if (contentItem.Analysis != null)
        {
            summaryDto = new ContentAnalysisSummaryDto
            {
                Category = contentItem.Analysis.Category,
                PrimaryTopic = contentItem.Analysis.PrimaryTopic,
                InformationDepth = contentItem.Analysis.InformationDepth,
                EducationalValue = contentItem.Analysis.EducationalValue,
                GoalRelevance = contentItem.Analysis.GoalRelevance,
                RepetitionScore = contentItem.Analysis.RepetitionScore,
                Summary = contentItem.Analysis.Summary,
                KeyTakeaways = JsonSerializer.Deserialize<List<string>>(contentItem.Analysis.KeyTakeawaysJson) ?? new()
            };
        }

        return Ok(new ContentIngestionResponseDto
        {
            EventId = contentEvent.Id,
            SessionId = session.Id,
            ContentItemId = contentItem.Id,
            AnalysisQueued = queued,
            ImmediateAnalysis = summaryDto
        });
    }

    [HttpPost("session/heartbeat")]
    public async Task<ActionResult<SessionHeartbeatResponseDto>> Heartbeat([FromBody] SessionHeartbeatDto dto)
    {
        var userId = _currentUserService.UserId!.Value;

        ContentSession? session = null;
        if (dto.SessionId.HasValue)
        {
            session = await _dbContext.ContentSessions.FirstOrDefaultAsync(s => s.Id == dto.SessionId.Value && s.UserId == userId);
        }

        if (session == null)
        {
            session = new ContentSession
            {
                UserId = userId,
                StartedAtUtc = DateTime.UtcNow,
                Status = SessionStatus.Active
            };
            _dbContext.ContentSessions.Add(session);
        }

        session.ActiveScrollDurationSeconds += dto.ActiveScrollSecondsIncrement;
        session.IdleDurationSeconds += dto.IdleSecondsIncrement;
        session.TotalDurationSeconds = session.ActiveScrollDurationSeconds + session.IdleDurationSeconds;

        await _dbContext.SaveChangesAsync();

        // Evaluate real-time intervention
        var eval = await _interventionEngine.EvaluateSessionInterventionAsync(userId, session.Id);

        return Ok(new SessionHeartbeatResponseDto
        {
            SessionId = session.Id,
            TotalDurationSeconds = session.TotalDurationSeconds,
            ActiveScrollDurationSeconds = session.ActiveScrollDurationSeconds,
            TriggerIntervention = eval.ShouldIntervene,
            Intervention = eval.ShouldIntervene ? eval : null
        });
    }

    [HttpGet("items")]
    public async Task<ActionResult<List<ContentItemDetailDto>>> GetContentItems([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = _currentUserService.UserId!.Value;

        var items = await _dbContext.ContentEvents
            .Include(e => e.ContentItem)
                .ThenInclude(ci => ci!.Analysis)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.TimestampUtc)
            .ToListAsync();

        var result = items.Select(e => new ContentItemDetailDto
        {
            Id = e.ContentItem!.Id,
            Url = e.ContentItem.Url,
            PlatformContentId = e.ContentItem.PlatformContentId,
            Title = e.ContentItem.Title,
            Creator = e.ContentItem.Creator,
            Caption = e.ContentItem.Caption,
            DurationSeconds = e.ContentItem.DurationSeconds,
            SourceProvider = e.ContentItem.SourceProvider,
            CreatedAtUtc = e.TimestampUtc,
            Analysis = e.ContentItem.Analysis != null ? new ContentAnalysisSummaryDto
            {
                Category = e.ContentItem.Analysis.Category,
                PrimaryTopic = e.ContentItem.Analysis.PrimaryTopic,
                InformationDepth = e.ContentItem.Analysis.InformationDepth,
                EducationalValue = e.ContentItem.Analysis.EducationalValue,
                GoalRelevance = e.ContentItem.Analysis.GoalRelevance,
                RepetitionScore = e.ContentItem.Analysis.RepetitionScore,
                Summary = e.ContentItem.Analysis.Summary,
                KeyTakeaways = JsonSerializer.Deserialize<List<string>>(e.ContentItem.Analysis.KeyTakeawaysJson) ?? new()
            } : null
        }).ToList();

        return Ok(result);
    }

    private static string ComputeSha256(string raw)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InterventionsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IInterventionEngine _interventionEngine;

    public InterventionsController(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IInterventionEngine interventionEngine)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _interventionEngine = interventionEngine;
    }

    [HttpGet("evaluate")]
    public async Task<ActionResult<InterventionEvaluationResponseDto>> Evaluate([FromQuery] Guid sessionId)
    {
        var userId = _currentUserService.UserId!.Value;
        var eval = await _interventionEngine.EvaluateSessionInterventionAsync(userId, sessionId);
        return Ok(eval);
    }

    [HttpPost("action")]
    public async Task<IActionResult> RecordAction([FromBody] InterventionActionDto dto)
    {
        var userId = _currentUserService.UserId!.Value;
        var success = await _interventionEngine.RecordInterventionActionAsync(userId, dto.InterventionId, dto.Response);
        if (!success) return NotFound(new { message = "Intervention record not found." });
        return Ok(new { message = "Intervention response recorded successfully." });
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<Intervention>>> GetHistory()
    {
        var userId = _currentUserService.UserId!.Value;
        var history = await _dbContext.Interventions
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.TriggeredAtUtc)
            .Take(20)
            .ToListAsync();

        return Ok(history);
    }
}
