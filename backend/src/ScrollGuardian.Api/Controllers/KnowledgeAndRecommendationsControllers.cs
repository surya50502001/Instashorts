using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Enums;

namespace ScrollGuardian.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class KnowledgeController : ControllerBase
{
    private readonly IKnowledgeRetentionService _retentionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<SubmitKnowledgeAnswerDto> _answerValidator;

    public KnowledgeController(
        IKnowledgeRetentionService retentionService,
        ICurrentUserService currentUserService,
        IValidator<SubmitKnowledgeAnswerDto> answerValidator)
    {
        _retentionService = retentionService;
        _currentUserService = currentUserService;
        _answerValidator = answerValidator;
    }

    [HttpGet("check")]
    public async Task<ActionResult<KnowledgeCheckDetailDto>> GetPendingCheck()
    {
        var userId = _currentUserService.UserId!.Value;
        var check = await _retentionService.GetPendingOrNextKnowledgeCheckAsync(userId);
        if (check == null)
        {
            return Ok(new { message = "No pending knowledge checks at this time. Consume more educational content to unlock new checks." });
        }
        return Ok(check);
    }

    [HttpPost("answer")]
    public async Task<ActionResult<SubmitAnswerResultDto>> SubmitAnswer([FromBody] SubmitKnowledgeAnswerDto dto)
    {
        var validation = await _answerValidator.ValidateAsync(dto);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        var userId = _currentUserService.UserId!.Value;
        var result = await _retentionService.SubmitAnswerAsync(userId, dto);
        return Ok(result);
    }

    [HttpGet("map")]
    public async Task<ActionResult<List<KnowledgeMapNodeDto>>> GetKnowledgeMap()
    {
        var userId = _currentUserService.UserId!.Value;
        var map = await _retentionService.GetKnowledgeMapAsync(userId);
        return Ok(map);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<RetentionSummaryDto>> GetRetentionSummary()
    {
        var userId = _currentUserService.UserId!.Value;
        var summary = await _retentionService.GetRetentionSummaryAsync(userId);
        return Ok(summary);
    }
}

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationEngine _recommendationEngine;
    private readonly ICurrentUserService _currentUserService;

    public RecommendationsController(
        IRecommendationEngine recommendationEngine,
        ICurrentUserService currentUserService)
    {
        _recommendationEngine = recommendationEngine;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<List<RecommendationResponseDto>>> GetRecommendations([FromQuery] int limit = 5)
    {
        var userId = _currentUserService.UserId!.Value;
        var recs = await _recommendationEngine.GetRecommendationsAsync(userId, limit);
        return Ok(recs);
    }

    [HttpGet("find-useful")]
    public async Task<ActionResult<RecommendationResponseDto>> FindSomethingUseful()
    {
        var userId = _currentUserService.UserId!.Value;
        var rec = await _recommendationEngine.GetFindSomethingUsefulRecommendationAsync(userId);
        if (rec == null)
        {
            return Ok(new { message = "No tailored recommendations available yet. Configure your goals to unlock personalized recommendations." });
        }
        return Ok(rec);
    }

    [HttpPost("{id}/feedback")]
    public async Task<IActionResult> RecordFeedback(Guid id, [FromBody] RecommendationFeedbackDto dto)
    {
        var userId = _currentUserService.UserId!.Value;
        var success = await _recommendationEngine.RecordInteractionAsync(userId, id, dto.InteractionType);
        if (!success) return NotFound();
        return Ok(new { message = "Feedback recorded." });
    }
}
