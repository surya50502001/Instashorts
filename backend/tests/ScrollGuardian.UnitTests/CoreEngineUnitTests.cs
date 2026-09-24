using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Entities;
using ScrollGuardian.Domain.Enums;
using ScrollGuardian.Infrastructure.Persistence;
using ScrollGuardian.Infrastructure.Services;
using Xunit;

namespace ScrollGuardian.UnitTests;

public class CoreEngineUnitTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RecommendationEngine _recommendationEngine;
    private readonly KnowledgeRetentionService _retentionService;
    private readonly InterventionEngine _interventionEngine;
    private readonly AnalyticsAggregationService _analyticsService;

    public CoreEngineUnitTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _recommendationEngine = new RecommendationEngine(_dbContext);
        _retentionService = new KnowledgeRetentionService(_dbContext);
        _interventionEngine = new InterventionEngine(_dbContext, _recommendationEngine, _retentionService);
        _analyticsService = new AnalyticsAggregationService(_dbContext, _recommendationEngine);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task RetentionSummary_WhenNoAnswers_ShouldReturnHonestEmptyState()
    {
        var userId = Guid.NewGuid();
        var summary = await _retentionService.GetRetentionSummaryAsync(userId);

        summary.TotalQuestionsAnswered.Should().Be(0);
        summary.OverallRetentionRate.Should().Be(0);
        summary.RetentionStatusMessage.Should().Contain("Not enough data yet");
    }

    [Fact]
    public async Task SubmitAnswer_ShouldUpdateAccuracyAndMasteryProperly()
    {
        var userId = Guid.NewGuid();
        var topic = new ContentTopic { Id = Guid.NewGuid(), Name = "EF Core", Slug = "efcore" };
        _dbContext.ContentTopics.Add(topic);

        var check = new KnowledgeCheck { Id = Guid.NewGuid(), UserId = userId, TopicId = topic.Id, Status = KnowledgeCheckStatus.Pending };
        var question = new KnowledgeQuestion
        {
            Id = Guid.NewGuid(),
            KnowledgeCheckId = check.Id,
            TopicId = topic.Id,
            QuestionText = "How to do read-only queries in EF Core?",
            OptionsJson = "[\".AsNoTracking()\", \".Save()\", \".Drop()\", \".Cast()\"]",
            CorrectOptionIndex = 0,
            Explanation = "Use .AsNoTracking() to bypass change tracker."
        };
        _dbContext.KnowledgeChecks.Add(check);
        _dbContext.KnowledgeQuestions.Add(question);
        await _dbContext.SaveChangesAsync();

        var result = await _retentionService.SubmitAnswerAsync(userId, new SubmitKnowledgeAnswerDto
        {
            CheckId = check.Id,
            QuestionId = question.Id,
            SelectedOptionIndex = 0,
            ResponseTimeSeconds = 4.2
        });

        result.IsCorrect.Should().BeTrue();
        result.IsCheckCompleted.Should().BeTrue();
        result.TotalCheckScorePercentage.Should().Be(100.0);

        var userTopic = await _dbContext.UserTopics.FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TopicId == topic.Id);
        userTopic.Should().NotBeNull();
        userTopic!.KnowledgeChecksCorrect.Should().Be(1);
        userTopic.MasteryScore.Should().Be(100.0);
    }

    [Fact]
    public async Task Recommendations_ShouldAlignWithUserGoals()
    {
        var userId = Guid.NewGuid();
        _dbContext.Goals.Add(new UserGoal
        {
            UserId = userId,
            Category = "Programming",
            Title = "Become a better .NET developer",
            Priority = 1,
            IsActive = true
        });
        await _dbContext.SaveChangesAsync();

        var recs = await _recommendationEngine.GetRecommendationsAsync(userId, limit: 3);

        recs.Should().NotBeEmpty();
        (recs[0].Title.Contains(".NET") || recs[0].Title.Contains("EF Core")).Should().BeTrue();
        recs[0].ReasonDescription.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DashboardSummary_WhenNoActivity_ShouldReturnZeroesWithoutFakeData()
    {
        var userId = Guid.NewGuid();
        var summary = await _analyticsService.GetDashboardSummaryAsync(userId);

        summary.ActiveScrollMinutes.Should().Be(0);
        summary.LearningMinutes.Should().Be(0);
        summary.EntertainmentMinutes.Should().Be(0);
        summary.ItemsConsumedCount.Should().Be(0);
        summary.DominantCategory.Should().Be("None");
        summary.Timeline.Should().BeEmpty();
    }
}
