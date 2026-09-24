using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Entities;
using ScrollGuardian.Domain.Enums;
using ScrollGuardian.Infrastructure.Persistence;

namespace ScrollGuardian.Infrastructure.Services;

public class KnowledgeRetentionService : IKnowledgeRetentionService
{
    private readonly ApplicationDbContext _dbContext;

    public KnowledgeRetentionService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<KnowledgeCheckDetailDto?> GetPendingOrNextKnowledgeCheckAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var check = await _dbContext.KnowledgeChecks
            .Include(kc => kc.Questions)
            .Include(kc => kc.Topic)
            .Include(kc => kc.ContentItem)
            .Where(kc => kc.UserId == userId && kc.Status == KnowledgeCheckStatus.Pending)
            .OrderBy(kc => kc.TriggeredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (check == null || check.Questions.Count == 0)
        {
            return null;
        }

        return new KnowledgeCheckDetailDto
        {
            CheckId = check.Id,
            TopicName = check.Topic?.Name ?? "General Concepts",
            ContentTitle = check.ContentItem?.Title,
            ContentUrl = check.ContentItem?.Url,
            Questions = check.Questions.Select(q => new KnowledgeQuestionDto
            {
                QuestionId = q.Id,
                QuestionText = q.QuestionText,
                Options = JsonSerializer.Deserialize<List<string>>(q.OptionsJson) ?? new List<string>(),
                Difficulty = q.Difficulty
            }).ToList()
        };
    }

    public async Task<SubmitAnswerResultDto> SubmitAnswerAsync(Guid userId, SubmitKnowledgeAnswerDto dto, CancellationToken cancellationToken = default)
    {
        var check = await _dbContext.KnowledgeChecks
            .Include(kc => kc.Questions)
            .Include(kc => kc.Answers)
            .FirstOrDefaultAsync(kc => kc.Id == dto.CheckId && kc.UserId == userId, cancellationToken);

        if (check == null)
        {
            throw new KeyNotFoundException("Knowledge check not found.");
        }

        var question = check.Questions.FirstOrDefault(q => q.Id == dto.QuestionId);
        if (question == null)
        {
            throw new KeyNotFoundException("Question not found.");
        }

        bool isCorrect = (dto.SelectedOptionIndex == question.CorrectOptionIndex);

        var answer = new KnowledgeAnswer
        {
            KnowledgeCheckId = check.Id,
            KnowledgeQuestionId = question.Id,
            UserId = userId,
            SelectedOptionIndex = dto.SelectedOptionIndex,
            IsCorrect = isCorrect,
            ResponseTimeSeconds = dto.ResponseTimeSeconds,
            AnsweredAtUtc = DateTime.UtcNow
        };

        _dbContext.KnowledgeAnswers.Add(answer);

        // Update Topic Mastery & Learning Progress
        double updatedMastery = 50.0;
        double updatedRetention = 50.0;

        if (question.TopicId.HasValue)
        {
            var userTopic = await _dbContext.UserTopics
                .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TopicId == question.TopicId.Value, cancellationToken);

            if (userTopic == null)
            {
                userTopic = new UserTopic
                {
                    UserId = userId,
                    TopicId = question.TopicId.Value,
                    ContentCount = 1,
                    KnowledgeChecksAttempted = 1,
                    KnowledgeChecksCorrect = isCorrect ? 1 : 0,
                    MasteryScore = isCorrect ? 100.0 : 0.0,
                    LastExposedAtUtc = DateTime.UtcNow
                };
                _dbContext.UserTopics.Add(userTopic);
                updatedMastery = userTopic.MasteryScore;
            }
            else
            {
                userTopic.KnowledgeChecksAttempted++;
                if (isCorrect) userTopic.KnowledgeChecksCorrect++;
                userTopic.MasteryScore = Math.Round((double)userTopic.KnowledgeChecksCorrect / userTopic.KnowledgeChecksAttempted * 100, 1);
                userTopic.LastExposedAtUtc = DateTime.UtcNow;
                updatedMastery = userTopic.MasteryScore;
            }

            var progress = await _dbContext.LearningProgresses
                .FirstOrDefaultAsync(lp => lp.UserId == userId && lp.TopicId == question.TopicId.Value, cancellationToken);

            if (progress == null)
            {
                progress = new LearningProgress
                {
                    UserId = userId,
                    TopicId = question.TopicId.Value,
                    TotalQuestionsAnswered = 1,
                    TotalQuestionsCorrect = isCorrect ? 1 : 0,
                    RetentionScore = isCorrect ? 80.0 : 40.0,
                    ConsistencyScore = 60.0,
                    ExposureScore = 50.0,
                    LastCheckAtUtc = DateTime.UtcNow
                };
                _dbContext.LearningProgresses.Add(progress);
                updatedRetention = progress.RetentionScore;
            }
            else
            {
                progress.TotalQuestionsAnswered++;
                if (isCorrect) progress.TotalQuestionsCorrect++;
                progress.RetentionScore = Math.Round((double)progress.TotalQuestionsCorrect / progress.TotalQuestionsAnswered * 100, 1);
                progress.LastCheckAtUtc = DateTime.UtcNow;
                updatedRetention = progress.RetentionScore;
            }
        }

        // Check if all questions in this check are now answered
        var allAnswers = check.Answers.ToList();
        if (!allAnswers.Any(a => a.Id == answer.Id))
        {
            allAnswers.Add(answer);
        }

        bool isCheckCompleted = allAnswers.Count >= check.Questions.Count;
        double? finalScore = null;

        if (isCheckCompleted)
        {
            check.Status = KnowledgeCheckStatus.Completed;
            check.CompletedAtUtc = DateTime.UtcNow;

            int correctCount = allAnswers.Count(a => a.IsCorrect);
            finalScore = Math.Round((double)correctCount / check.Questions.Count * 100, 1);
            check.ScorePercentage = finalScore;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SubmitAnswerResultDto
        {
            IsCorrect = isCorrect,
            CorrectOptionIndex = question.CorrectOptionIndex,
            Explanation = question.Explanation,
            UpdatedMasteryScore = updatedMastery,
            UpdatedRetentionScore = updatedRetention,
            IsCheckCompleted = isCheckCompleted,
            TotalCheckScorePercentage = finalScore
        };
    }

    public async Task<List<KnowledgeMapNodeDto>> GetKnowledgeMapAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userTopics = await _dbContext.UserTopics
            .Include(ut => ut.Topic)
                .ThenInclude(t => t!.Category)
            .Include(ut => ut.Topic)
                .ThenInclude(t => t!.ParentTopic)
            .Where(ut => ut.UserId == userId)
            .ToListAsync(cancellationToken);

        var progressList = await _dbContext.LearningProgresses
            .Where(lp => lp.UserId == userId)
            .ToListAsync(cancellationToken);

        var progressDict = progressList.ToDictionary(p => p.TopicId);

        return userTopics.Select(ut =>
        {
            progressDict.TryGetValue(ut.TopicId, out var prog);
            return new KnowledgeMapNodeDto
            {
                TopicId = ut.TopicId,
                Category = ut.Topic?.Category?.Name ?? "General",
                TopicName = ut.Topic?.Name ?? "Topic",
                ParentTopicName = ut.Topic?.ParentTopic?.Name,
                ContentConsumedCount = ut.ContentCount,
                TotalTimeSpentMinutes = Math.Round(ut.TotalTimeSpentSeconds / 60.0, 1),
                KnowledgeChecksAttempted = ut.KnowledgeChecksAttempted,
                KnowledgeChecksCorrect = ut.KnowledgeChecksCorrect,
                MasteryScore = ut.MasteryScore,
                RetentionScore = prog?.RetentionScore ?? (ut.KnowledgeChecksAttempted > 0 ? ut.MasteryScore : 0.0),
                LastExposedAtUtc = ut.LastExposedAtUtc
            };
        }).ToList();
    }

    public async Task<RetentionSummaryDto> GetRetentionSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var answers = await _dbContext.KnowledgeAnswers
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);

        var userTopics = await _dbContext.UserTopics
            .Where(ut => ut.UserId == userId)
            .ToListAsync(cancellationToken);

        int totalAnswered = answers.Count;
        int totalCorrect = answers.Count(a => a.IsCorrect);

        if (totalAnswered == 0)
        {
            return new RetentionSummaryDto
            {
                OverallRetentionRate = 0,
                TotalQuestionsAnswered = 0,
                TotalQuestionsCorrect = 0,
                MasteredTopicsCount = 0,
                DevelopingTopicsCount = 0,
                RetentionStatusMessage = "Not enough data yet. Complete knowledge checks to measure actual learning retention."
            };
        }

        double rate = Math.Round((double)totalCorrect / totalAnswered * 100, 1);
        int mastered = userTopics.Count(ut => ut.MasteryScore >= 75.0 && ut.KnowledgeChecksAttempted >= 3);
        int developing = userTopics.Count(ut => ut.MasteryScore < 75.0 || ut.KnowledgeChecksAttempted < 3);

        string message = rate >= 80.0
            ? "High retention. Spaced checks show strong understanding across your active topics."
            : rate >= 60.0
                ? "Moderate retention. Continued exposure and targeted reviews will solidify mastery."
                : "Developing retention. Consider reviewing foundational summaries in weak topics.";

        return new RetentionSummaryDto
        {
            OverallRetentionRate = rate,
            TotalQuestionsAnswered = totalAnswered,
            TotalQuestionsCorrect = totalCorrect,
            MasteredTopicsCount = mastered,
            DevelopingTopicsCount = developing,
            RetentionStatusMessage = message
        };
    }
}
