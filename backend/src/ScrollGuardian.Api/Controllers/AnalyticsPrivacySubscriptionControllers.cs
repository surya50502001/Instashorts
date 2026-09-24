using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Enums;

namespace ScrollGuardian.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsAggregationService _analyticsService;
    private readonly ICurrentUserService _currentUserService;

    public AnalyticsController(
        IAnalyticsAggregationService analyticsService,
        ICurrentUserService currentUserService)
    {
        _analyticsService = analyticsService;
        _currentUserService = currentUserService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboard([FromQuery] string? date = null)
    {
        var userId = _currentUserService.UserId!.Value;
        DateOnly? targetDate = null;
        if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var parsed))
        {
            targetDate = parsed;
        }

        var summary = await _analyticsService.GetDashboardSummaryAsync(userId, targetDate);
        return Ok(summary);
    }

    [HttpGet("daily")]
    public async Task<ActionResult<DailyAnalyticsDto>> GetDailyAnalytics([FromQuery] string? date = null)
    {
        var userId = _currentUserService.UserId!.Value;
        var targetDate = !string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var analytics = await _analyticsService.GetDailyAnalyticsAsync(userId, targetDate);
        return Ok(analytics);
    }

    [HttpGet("weekly")]
    public async Task<ActionResult<WeeklyReviewDto>> GetWeeklyReview([FromQuery] string? startDate = null)
    {
        var userId = _currentUserService.UserId!.Value;
        var targetStart = !string.IsNullOrWhiteSpace(startDate) && DateOnly.TryParse(startDate, out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6));

        var review = await _analyticsService.GetWeeklyReviewAsync(userId, targetStart);
        return Ok(review);
    }
}

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PrivacyController : ControllerBase
{
    private readonly IDataPrivacyService _privacyService;
    private readonly ICurrentUserService _currentUserService;

    public PrivacyController(
        IDataPrivacyService privacyService,
        ICurrentUserService currentUserService)
    {
        _privacyService = privacyService;
        _currentUserService = currentUserService;
    }

    [HttpGet("export")]
    public async Task<ActionResult<UserDataExportDto>> ExportData()
    {
        var userId = _currentUserService.UserId!.Value;
        var export = await _privacyService.ExportAllUserDataAsync(userId);
        return Ok(export);
    }

    [HttpDelete("event/{id}")]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        var userId = _currentUserService.UserId!.Value;
        var success = await _privacyService.DeleteContentEventAsync(userId, id);
        if (!success) return NotFound();
        return Ok(new { message = "Content event deleted successfully." });
    }

    [HttpPost("purge")]
    public async Task<IActionResult> PurgeHistory([FromBody] PurgeHistoryRequestDto dto)
    {
        var userId = _currentUserService.UserId!.Value;
        var success = await _privacyService.PurgeActivityHistoryAsync(userId, dto.BeforeDateUtc);
        return Ok(new { message = "Activity history purged successfully." });
    }

    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = _currentUserService.UserId!.Value;
        var success = await _privacyService.DeleteUserAccountAsync(userId);
        if (!success) return NotFound();
        return Ok(new { message = "Account and all associated personal data have been completely deleted." });
    }
}

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ICurrentUserService _currentUserService;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        ICurrentUserService currentUserService)
    {
        _subscriptionService = subscriptionService;
        _currentUserService = currentUserService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<SubscriptionStatusDto>> GetStatus()
    {
        var userId = _currentUserService.UserId!.Value;
        var status = await _subscriptionService.GetSubscriptionStatusAsync(userId);
        return Ok(status);
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutSessionResponseDto>> CreateCheckout([FromBody] CreateCheckoutRequestDto dto)
    {
        var userId = _currentUserService.UserId!.Value;
        var checkout = await _subscriptionService.CreateCheckoutSessionAsync(userId, dto.Plan);
        return Ok(checkout);
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var json = await reader.ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();
        var handled = await _subscriptionService.ProcessWebhookEventAsync(json, signature);
        return handled ? Ok() : BadRequest();
    }
}

public class CreateCheckoutRequestDto
{
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Pro;
}
