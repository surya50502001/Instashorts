using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Entities;
using ScrollGuardian.Domain.Enums;
using ScrollGuardian.Infrastructure.Persistence;

namespace ScrollGuardian.Infrastructure.BackgroundJobs;

public class ContentProcessingQueueItem
{
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public Guid ContentItemId { get; set; }
}

public class ContentProcessingChannel
{
    private readonly Channel<ContentProcessingQueueItem> _channel;

    public ContentProcessingChannel()
    {
        var options = new BoundedChannelOptions(5000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<ContentProcessingQueueItem>(options);
    }

    public async ValueTask QueueItemAsync(ContentProcessingQueueItem item, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(item, cancellationToken);
    }

    public IAsyncEnumerable<ContentProcessingQueueItem> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}

public class ContentProcessingWorker : BackgroundService
{
    private readonly ContentProcessingChannel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ContentProcessingWorker> _logger;

    public ContentProcessingWorker(
        ContentProcessingChannel channel,
        IServiceProvider serviceProvider,
        ILogger<ContentProcessingWorker> logger)
    {
        _channel = channel;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScrollGuardian Content Processing Worker started.");

        await foreach (var item in _channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var aiAnalyzer = scope.ServiceProvider.GetRequiredService<IAIContentAnalyzer>();
                var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsAggregationService>();

                var contentItem = await dbContext.ContentItems
                    .Include(ci => ci.Analysis)
                    .FirstOrDefaultAsync(ci => ci.Id == item.ContentItemId, stoppingToken);

                if (contentItem == null) continue;

                // If content is not analyzed yet
                if (contentItem.Analysis == null)
                {
                    // Fetch user's goal keywords for personalized relevance scoring
                    var userGoals = await dbContext.Goals
                        .Where(g => g.UserId == item.UserId && g.IsActive)
                        .Select(g => g.Title + " " + g.Category)
                        .ToListAsync(stoppingToken);

                    var analysisResult = await aiAnalyzer.AnalyzeContentAsync(
                        contentItem.Title,
                        contentItem.Creator,
                        contentItem.Caption,
                        contentItem.RawTranscript,
                        userGoals,
                        stoppingToken);

                    var analysis = new ContentAnalysis
                    {
                        ContentItemId = contentItem.Id,
                        Category = analysisResult.Category,
                        PrimaryTopic = analysisResult.PrimaryTopic,
                        SecondaryTopicsJson = JsonSerializer.Serialize(analysisResult.SecondaryTopics),
                        InformationDepth = analysisResult.InformationDepth,
                        EducationalValue = analysisResult.EducationalValue,
                        Novelty = analysisResult.Novelty,
                        GoalRelevance = analysisResult.GoalRelevance,
                        RepetitionScore = analysisResult.RepetitionScore,
                        PracticalValue = analysisResult.PracticalValue,
                        SourceConfidence = analysisResult.SourceConfidence,
                        Summary = analysisResult.Summary,
                        KeyTakeawaysJson = JsonSerializer.Serialize(analysisResult.KeyTakeaways),
                        ClaimsRequiringVerificationJson = JsonSerializer.Serialize(analysisResult.ClaimsRequiringVerification),
                        AiModelUsed = analysisResult.ModelUsed,
                        PromptTokens = analysisResult.PromptTokens,
                        CompletionTokens = analysisResult.CompletionTokens,
                        CostEstimatedUsd = analysisResult.EstimatedCostUsd,
                        RawAiResponseJson = JsonSerializer.Serialize(analysisResult)
                    };

                    dbContext.ContentAnalyses.Add(analysis);
                    contentItem.AnalyzedAtUtc = DateTime.UtcNow;

                    // Match or create ContentTopic
                    var topicSlug = analysisResult.PrimaryTopic.ToLowerInvariant().Replace(" ", "-");
                    var existingTopic = await dbContext.ContentTopics
                        .FirstOrDefaultAsync(t => t.Slug == topicSlug || t.Name.ToLower() == analysisResult.PrimaryTopic.ToLower(), stoppingToken);

                    Guid topicId;
                    if (existingTopic != null)
                    {
                        topicId = existingTopic.Id;
                    }
                    else
                    {
                        var category = await dbContext.ContentCategories
                            .FirstOrDefaultAsync(c => c.Name.ToLower() == analysisResult.Category.ToLower(), stoppingToken);

                        var newTopic = new ContentTopic
                        {
                            CategoryId = category?.Id ?? (await dbContext.ContentCategories.FirstAsync(c => c.Slug == "other", stoppingToken)).Id,
                            Name = analysisResult.PrimaryTopic,
                            Slug = topicSlug,
                            Description = $"Topics surrounding {analysisResult.PrimaryTopic}",
                            KeywordsJson = JsonSerializer.Serialize(analysisResult.SecondaryTopics)
                        };
                        dbContext.ContentTopics.Add(newTopic);
                        await dbContext.SaveChangesAsync(stoppingToken);
                        topicId = newTopic.Id;
                    }

                    // Create Knowledge Check and Questions if educational
                    if (analysisResult.GeneratedQuestions.Count > 0)
                    {
                        var knowledgeCheck = new KnowledgeCheck
                        {
                            UserId = item.UserId,
                            ContentItemId = contentItem.Id,
                            TopicId = topicId,
                            Status = KnowledgeCheckStatus.Pending,
                            TriggeredAtUtc = DateTime.UtcNow
                        };
                        dbContext.KnowledgeChecks.Add(knowledgeCheck);

                        foreach (var gq in analysisResult.GeneratedQuestions)
                        {
                            var q = new KnowledgeQuestion
                            {
                                KnowledgeCheckId = knowledgeCheck.Id,
                                ContentItemId = contentItem.Id,
                                TopicId = topicId,
                                QuestionText = gq.QuestionText,
                                OptionsJson = JsonSerializer.Serialize(gq.Options),
                                CorrectOptionIndex = gq.CorrectOptionIndex,
                                Explanation = gq.Explanation,
                                Difficulty = gq.Difficulty
                            };
                            dbContext.KnowledgeQuestions.Add(q);
                        }
                    }

                    // Update UserTopic tracking
                    var userTopic = await dbContext.UserTopics
                        .FirstOrDefaultAsync(ut => ut.UserId == item.UserId && ut.TopicId == topicId, stoppingToken);

                    if (userTopic == null)
                    {
                        userTopic = new UserTopic
                        {
                            UserId = item.UserId,
                            TopicId = topicId,
                            ContentCount = 1,
                            TotalTimeSpentSeconds = 30,
                            MasteryScore = 20,
                            LastExposedAtUtc = DateTime.UtcNow
                        };
                        dbContext.UserTopics.Add(userTopic);
                    }
                    else
                    {
                        userTopic.ContentCount++;
                        userTopic.LastExposedAtUtc = DateTime.UtcNow;
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                }

                // Trigger aggregate rebuild for the current date
                await analyticsService.RebuildDailySummaryAsync(item.UserId, DateOnly.FromDateTime(DateTime.UtcNow), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing content item {ContentItemId}", item.ContentItemId);
            }
        }
    }
}
