using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Entities;
using ScrollGuardian.Infrastructure.Persistence;

namespace ScrollGuardian.Infrastructure.Services;

public class AnalyticsAggregationService : IAnalyticsAggregationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRecommendationEngine _recommendationEngine;

    public AnalyticsAggregationService(
        ApplicationDbContext dbContext,
        IRecommendationEngine recommendationEngine)
    {
        _dbContext = dbContext;
        _recommendationEngine = recommendationEngine;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid userId, DateOnly? date = null, CancellationToken cancellationToken = default)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var startUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = targetDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var eventsToday = await _dbContext.ContentEvents
            .Include(e => e.ContentItem)
                .ThenInclude(ci => ci!.Analysis)
            .Where(e => e.UserId == userId && e.TimestampUtc >= startUtc && e.TimestampUtc <= endUtc)
            .OrderByDescending(e => e.TimestampUtc)
            .ToListAsync(cancellationToken);

        var sessionsToday = await _dbContext.ContentSessions
            .Where(s => s.UserId == userId && s.StartedAtUtc >= startUtc && s.StartedAtUtc <= endUtc)
            .ToListAsync(cancellationToken);

        var answersToday = await _dbContext.KnowledgeAnswers
            .Where(a => a.UserId == userId && a.AnsweredAtUtc >= startUtc && a.AnsweredAtUtc <= endUtc)
            .ToListAsync(cancellationToken);

        double totalScrollSeconds = eventsToday.Sum(e => e.TimeSpentSeconds);
        double learningSeconds = eventsToday
            .Where(e => e.ContentItem?.Analysis?.Category is "Programming" or "Education" or "Science" or "Finance" or "Fitness" or "Business")
            .Sum(e => e.TimeSpentSeconds);
        double entertainmentSeconds = eventsToday
            .Where(e => e.ContentItem?.Analysis?.Category is "Entertainment" or "Comedy" or "Gaming" or "Music" or "Other")
            .Sum(e => e.TimeSpentSeconds);
        double goalRelevantSeconds = eventsToday
            .Where(e => e.IsGoalRelevant || (e.ContentItem?.Analysis != null && e.ContentItem.Analysis.GoalRelevance >= 50.0))
            .Sum(e => e.TimeSpentSeconds);

        // Calculate dominant category
        var dominantCategory = eventsToday.Count > 0
            ? eventsToday
                .GroupBy(e => e.ContentItem?.Analysis?.Category ?? "Other")
                .OrderByDescending(g => g.Sum(x => x.TimeSpentSeconds))
                .First().Key
            : "None";

        // Previous day comparison ("What's changing?")
        var prevDate = targetDate.AddDays(-1);
        var prevSummary = await _dbContext.DailySummaries
            .FirstOrDefaultAsync(ds => ds.UserId == userId && ds.Date == prevDate, cancellationToken);

        double? scrollPctChange = null;
        double? learningPctChange = null;
        double? goalPctChange = null;
        string trendText = "Not enough prior activity to calculate comparison.";

        if (prevSummary != null && prevSummary.TotalScrollSeconds > 0)
        {
            scrollPctChange = Math.Round(((totalScrollSeconds - prevSummary.TotalScrollSeconds) / prevSummary.TotalScrollSeconds) * 100, 1);
            if (prevSummary.LearningSeconds > 0)
            {
                learningPctChange = Math.Round(((learningSeconds - prevSummary.LearningSeconds) / prevSummary.LearningSeconds) * 100, 1);
            }
            if (prevSummary.GoalRelevantSeconds > 0)
            {
                goalPctChange = Math.Round(((goalRelevantSeconds - prevSummary.GoalRelevantSeconds) / prevSummary.GoalRelevantSeconds) * 100, 1);
            }

            if (learningPctChange.HasValue && learningPctChange.Value > 0)
            {
                trendText = $"You spent {Math.Abs(learningPctChange.Value)}% more time on goal-relevant learning today compared to yesterday.";
            }
            else if (scrollPctChange.HasValue && scrollPctChange.Value < 0)
            {
                trendText = $"Total scroll duration decreased by {Math.Abs(scrollPctChange.Value)}% compared to yesterday.";
            }
            else
            {
                trendText = "Activity is tracking steadily compared to yesterday.";
            }
        }

        // Timeline items
        var timeline = eventsToday.Take(15).Select(e => new ConsumptionTimelineEventDto
        {
            EventId = e.Id,
            TimestampUtc = e.TimestampUtc,
            Title = !string.IsNullOrWhiteSpace(e.ContentItem?.Title) ? e.ContentItem.Title : "Short-form Content",
            Creator = e.ContentItem?.Creator ?? "Unknown",
            Category = e.ContentItem?.Analysis?.Category ?? "Other",
            PrimaryTopic = e.ContentItem?.Analysis?.PrimaryTopic ?? "General",
            DurationSeconds = e.ContentItem?.DurationSeconds ?? 30,
            TimeSpentSeconds = e.TimeSpentSeconds,
            EducationalValue = e.ContentItem?.Analysis?.EducationalValue ?? 0,
            IsGoalRelevant = e.IsGoalRelevant || (e.ContentItem?.Analysis?.GoalRelevance >= 50.0),
            Url = e.ContentItem?.Url ?? ""
        }).ToList();

        // Top recommendation
        var topRecommendation = await _recommendationEngine.GetFindSomethingUsefulRecommendationAsync(userId, cancellationToken);

        double? accuracyToday = answersToday.Count > 0
            ? Math.Round((double)answersToday.Count(a => a.IsCorrect) / answersToday.Count * 100, 1)
            : null;

        return new DashboardSummaryDto
        {
            Date = targetDate,
            ActiveScrollMinutes = Math.Round(totalScrollSeconds / 60.0, 1),
            LearningMinutes = Math.Round(learningSeconds / 60.0, 1),
            EntertainmentMinutes = Math.Round(entertainmentSeconds / 60.0, 1),
            GoalRelevantMinutes = Math.Round(goalRelevantSeconds / 60.0, 1),
            TotalSessions = sessionsToday.Count,
            ItemsConsumedCount = eventsToday.Count,
            DominantCategory = dominantCategory,
            ScrollMinutesChangePct = scrollPctChange,
            LearningMinutesChangePct = learningPctChange,
            GoalRelevanceChangePct = goalPctChange,
            TrendComparisonText = trendText,
            Timeline = timeline,
            KnowledgeChecksToday = answersToday.Count,
            KnowledgeAccuracyToday = accuracyToday,
            TopRecommendation = topRecommendation
        };
    }

    public async Task<DailyAnalyticsDto> GetDailyAnalyticsAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var startUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var events = await _dbContext.ContentEvents
            .Include(e => e.ContentItem)
                .ThenInclude(ci => ci!.Analysis)
            .Where(e => e.UserId == userId && e.TimestampUtc >= startUtc && e.TimestampUtc <= endUtc)
            .ToListAsync(cancellationToken);

        var sessions = await _dbContext.ContentSessions
            .Where(s => s.UserId == userId && s.StartedAtUtc >= startUtc && s.StartedAtUtc <= endUtc)
            .ToListAsync(cancellationToken);

        var answers = await _dbContext.KnowledgeAnswers
            .Where(a => a.UserId == userId && a.AnsweredAtUtc >= startUtc && a.AnsweredAtUtc <= endUtc)
            .ToListAsync(cancellationToken);

        double totalScrollSeconds = events.Sum(e => e.TimeSpentSeconds);
        double learningSeconds = events.Where(e => e.ContentItem?.Analysis?.Category is "Programming" or "Education" or "Science" or "Finance" or "Fitness" or "Business").Sum(e => e.TimeSpentSeconds);
        double entertainmentSeconds = events.Where(e => e.ContentItem?.Analysis?.Category is "Entertainment" or "Comedy" or "Gaming" or "Music" or "Other").Sum(e => e.TimeSpentSeconds);
        double goalRelevantSeconds = events.Where(e => e.IsGoalRelevant || (e.ContentItem?.Analysis != null && e.ContentItem.Analysis.GoalRelevance >= 50.0)).Sum(e => e.TimeSpentSeconds);

        var categoryDistribution = events
            .GroupBy(e => e.ContentItem?.Analysis?.Category ?? "Other")
            .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.TimeSpentSeconds) / 60.0, 1));

        var topicDistribution = events
            .GroupBy(e => !string.IsNullOrEmpty(e.ContentItem?.Analysis?.PrimaryTopic) ? e.ContentItem.Analysis.PrimaryTopic : "General")
            .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.TimeSpentSeconds) / 60.0, 1));

        double longestSession = sessions.Count > 0 ? sessions.Max(s => s.TotalDurationSeconds) / 60.0 : 0.0;
        double avgSession = sessions.Count > 0 ? (sessions.Sum(s => s.TotalDurationSeconds) / sessions.Count) / 60.0 : 0.0;

        double repetition = events.Count > 0
            ? Math.Round(events.Average(e => e.ContentItem?.Analysis?.RepetitionScore ?? 30.0), 1)
            : 0.0;

        double accuracy = answers.Count > 0
            ? Math.Round((double)answers.Count(a => a.IsCorrect) / answers.Count * 100, 1)
            : 0.0;

        return new DailyAnalyticsDto
        {
            Date = date,
            TotalScrollMinutes = Math.Round(totalScrollSeconds / 60.0, 1),
            LearningMinutes = Math.Round(learningSeconds / 60.0, 1),
            EntertainmentMinutes = Math.Round(entertainmentSeconds / 60.0, 1),
            GoalRelevantMinutes = Math.Round(goalRelevantSeconds / 60.0, 1),
            SessionCount = sessions.Count,
            AverageSessionMinutes = Math.Round(avgSession, 1),
            LongestSessionMinutes = Math.Round(longestSession, 1),
            RepetitionScore = repetition,
            CategoryDistributionMinutes = categoryDistribution,
            TopicDistributionMinutes = topicDistribution,
            KnowledgeChecksCompleted = answers.Count,
            KnowledgeAccuracyRate = accuracy
        };
    }

    public async Task<WeeklyReviewDto> GetWeeklyReviewAsync(Guid userId, DateOnly weekStartDate, CancellationToken cancellationToken = default)
    {
        var weekEndDate = weekStartDate.AddDays(6);
        var startUtc = weekStartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = weekEndDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var events = await _dbContext.ContentEvents
            .Include(e => e.ContentItem)
                .ThenInclude(ci => ci!.Analysis)
            .Where(e => e.UserId == userId && e.TimestampUtc >= startUtc && e.TimestampUtc <= endUtc)
            .ToListAsync(cancellationToken);

        var answers = await _dbContext.KnowledgeAnswers
            .Where(a => a.UserId == userId && a.AnsweredAtUtc >= startUtc && a.AnsweredAtUtc <= endUtc)
            .ToListAsync(cancellationToken);

        double totalScrollSeconds = events.Sum(e => e.TimeSpentSeconds);
        double learningSeconds = events.Where(e => e.ContentItem?.Analysis?.Category is "Programming" or "Education" or "Science" or "Finance" or "Fitness" or "Business").Sum(e => e.TimeSpentSeconds);
        double goalRelevantSeconds = events.Where(e => e.IsGoalRelevant || (e.ContentItem?.Analysis != null && e.ContentItem.Analysis.GoalRelevance >= 50.0)).Sum(e => e.TimeSpentSeconds);

        double accuracy = answers.Count > 0
            ? Math.Round((double)answers.Count(a => a.IsCorrect) / answers.Count * 100, 1)
            : 0.0;

        // Daily Breakdown for the 7 days
        var dailyBreakdown = new List<DailyBreakdownDto>();
        for (int i = 0; i < 7; i++)
        {
            var curDate = weekStartDate.AddDays(i);
            var dayStart = curDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var dayEnd = curDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

            var dayEvents = events.Where(e => e.TimestampUtc >= dayStart && e.TimestampUtc <= dayEnd).ToList();
            dailyBreakdown.Add(new DailyBreakdownDto
            {
                Date = curDate,
                DayName = curDate.DayOfWeek.ToString(),
                ScrollMinutes = Math.Round(dayEvents.Sum(e => e.TimeSpentSeconds) / 60.0, 1),
                LearningMinutes = Math.Round(dayEvents.Where(e => e.ContentItem?.Analysis?.Category is "Programming" or "Education" or "Science" or "Finance" or "Fitness" or "Business").Sum(e => e.TimeSpentSeconds) / 60.0, 1),
                GoalRelevantMinutes = Math.Round(dayEvents.Where(e => e.IsGoalRelevant || (e.ContentItem?.Analysis?.GoalRelevance >= 50.0)).Sum(e => e.TimeSpentSeconds) / 60.0, 1)
            });
        }

        // Top topics
        var topTopics = events
            .Where(e => e.ContentItem?.Analysis != null && !string.IsNullOrEmpty(e.ContentItem.Analysis.PrimaryTopic))
            .GroupBy(e => new { Topic = e.ContentItem!.Analysis!.PrimaryTopic, Category = e.ContentItem.Analysis.Category })
            .Select(g => new TopicSummaryDto
            {
                TopicName = g.Key.Topic,
                Category = g.Key.Category,
                MinutesSpent = Math.Round(g.Sum(e => e.TimeSpentSeconds) / 60.0, 1),
                ItemsCount = g.Count()
            })
            .OrderByDescending(t => t.MinutesSpent)
            .Take(5)
            .ToList();

        // Dynamic, honest insights based strictly on real activity
        var insights = new List<string>();
        if (events.Count == 0)
        {
            insights.Add("No activity recorded for this week yet. Track short-form content to generate weekly insights.");
        }
        else
        {
            if (learningSeconds > 0)
            {
                var pct = Math.Round((learningSeconds / totalScrollSeconds) * 100, 1);
                insights.Add($"{pct}% of your short-form consumption was educational or goal-aligned this week.");
            }
            if (topTopics.Count > 0)
            {
                insights.Add($"Your most consumed topic was '{topTopics[0].TopicName}' in {topTopics[0].Category} ({topTopics[0].MinutesSpent} minutes).");
            }
            if (answers.Count > 0)
            {
                insights.Add($"You answered {answers.Count} knowledge retention questions with an accuracy rate of {accuracy}%.");
            }
        }

        return new WeeklyReviewDto
        {
            WeekStartDate = weekStartDate,
            WeekEndDate = weekEndDate,
            TotalScrollHours = Math.Round(totalScrollSeconds / 3600.0, 2),
            GoalRelevantHours = Math.Round(goalRelevantSeconds / 3600.0, 2),
            LearningHours = Math.Round(learningSeconds / 3600.0, 2),
            TotalKnowledgeChecks = answers.Count,
            AverageAccuracy = accuracy,
            DynamicInsights = insights,
            TopTopics = topTopics,
            DailyBreakdown = dailyBreakdown
        };
    }

    public async Task RebuildDailySummaryAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var startUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var events = await _dbContext.ContentEvents
            .Include(e => e.ContentItem)
                .ThenInclude(ci => ci!.Analysis)
            .Where(e => e.UserId == userId && e.TimestampUtc >= startUtc && e.TimestampUtc <= endUtc)
            .ToListAsync(cancellationToken);

        var sessions = await _dbContext.ContentSessions
            .Where(s => s.UserId == userId && s.StartedAtUtc >= startUtc && s.StartedAtUtc <= endUtc)
            .ToListAsync(cancellationToken);

        var answers = await _dbContext.KnowledgeAnswers
            .Where(a => a.UserId == userId && a.AnsweredAtUtc >= startUtc && a.AnsweredAtUtc <= endUtc)
            .ToListAsync(cancellationToken);

        var existingSummary = await _dbContext.DailySummaries
            .FirstOrDefaultAsync(ds => ds.UserId == userId && ds.Date == date, cancellationToken);

        double totalScrollSeconds = events.Sum(e => e.TimeSpentSeconds);
        double learningSeconds = events.Where(e => e.ContentItem?.Analysis?.Category is "Programming" or "Education" or "Science" or "Finance" or "Fitness" or "Business").Sum(e => e.TimeSpentSeconds);
        double entertainmentSeconds = events.Where(e => e.ContentItem?.Analysis?.Category is "Entertainment" or "Comedy" or "Gaming" or "Music" or "Other").Sum(e => e.TimeSpentSeconds);
        double goalRelevantSeconds = events.Where(e => e.IsGoalRelevant || (e.ContentItem?.Analysis != null && e.ContentItem.Analysis.GoalRelevance >= 50.0)).Sum(e => e.TimeSpentSeconds);

        var categoryBreakdown = events
            .GroupBy(e => e.ContentItem?.Analysis?.Category ?? "Other")
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TimeSpentSeconds));

        var topicBreakdown = events
            .GroupBy(e => !string.IsNullOrEmpty(e.ContentItem?.Analysis?.PrimaryTopic) ? e.ContentItem.Analysis.PrimaryTopic : "General")
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TimeSpentSeconds));

        var dominantCategory = events.Count > 0
            ? events.GroupBy(e => e.ContentItem?.Analysis?.Category ?? "Other").OrderByDescending(g => g.Sum(x => x.TimeSpentSeconds)).First().Key
            : "None";

        double repetition = events.Count > 0 ? events.Average(e => e.ContentItem?.Analysis?.RepetitionScore ?? 30.0) : 0.0;
        double accuracy = answers.Count > 0 ? ((double)answers.Count(a => a.IsCorrect) / answers.Count * 100) : 0.0;

        if (existingSummary == null)
        {
            existingSummary = new DailySummary
            {
                UserId = userId,
                Date = date,
                TotalScrollSeconds = totalScrollSeconds,
                LearningSeconds = learningSeconds,
                EntertainmentSeconds = entertainmentSeconds,
                GoalRelevantSeconds = goalRelevantSeconds,
                SessionCount = sessions.Count,
                ItemsConsumedCount = events.Count,
                KnowledgeChecksCompleted = answers.Count,
                KnowledgeChecksAccuracy = accuracy,
                RepetitionRatio = repetition,
                DominantCategory = dominantCategory,
                CategoryBreakdownJson = JsonSerializer.Serialize(categoryBreakdown),
                TopicBreakdownJson = JsonSerializer.Serialize(topicBreakdown)
            };
            _dbContext.DailySummaries.Add(existingSummary);
        }
        else
        {
            existingSummary.TotalScrollSeconds = totalScrollSeconds;
            existingSummary.LearningSeconds = learningSeconds;
            existingSummary.EntertainmentSeconds = entertainmentSeconds;
            existingSummary.GoalRelevantSeconds = goalRelevantSeconds;
            existingSummary.SessionCount = sessions.Count;
            existingSummary.ItemsConsumedCount = events.Count;
            existingSummary.KnowledgeChecksCompleted = answers.Count;
            existingSummary.KnowledgeChecksAccuracy = accuracy;
            existingSummary.RepetitionRatio = repetition;
            existingSummary.DominantCategory = dominantCategory;
            existingSummary.CategoryBreakdownJson = JsonSerializer.Serialize(categoryBreakdown);
            existingSummary.TopicBreakdownJson = JsonSerializer.Serialize(topicBreakdown);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
