using Microsoft.EntityFrameworkCore;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Entities;
using ScrollGuardian.Domain.Enums;
using ScrollGuardian.Infrastructure.Persistence;

namespace ScrollGuardian.Infrastructure.Services;

public class RecommendationEngine : IRecommendationEngine
{
    private readonly ApplicationDbContext _dbContext;

    public RecommendationEngine(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<RecommendationResponseDto>> GetRecommendationsAsync(Guid userId, int limit = 5, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Recommendations
            .Where(r => r.UserId == userId && (r.ExpiresAtUtc == null || r.ExpiresAtUtc > DateTime.UtcNow))
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.RelevanceScore)
            .Take(limit)
            .Select(r => new RecommendationResponseDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                Url = r.Url,
                ReasonDescription = r.ReasonDescription,
                SourceType = r.SourceType,
                TopicName = r.Topic != null ? r.Topic.Name : null,
                GoalTitle = r.Goal != null ? r.Goal.Title : null,
                RelevanceScore = r.RelevanceScore,
                Priority = r.Priority
            })
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            return existing;
        }

        // Generate fresh recommendations based on user goals and topics
        var generated = await GenerateGoalAlignedRecommendationsAsync(userId, limit, cancellationToken);
        return generated;
    }

    public async Task<RecommendationResponseDto?> GetFindSomethingUsefulRecommendationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = await GetRecommendationsAsync(userId, 1, cancellationToken);
        return list.FirstOrDefault();
    }

    public async Task<bool> RecordInteractionAsync(Guid userId, Guid recommendationId, RecommendationInteractionType interactionType, CancellationToken cancellationToken = default)
    {
        var recommendation = await _dbContext.Recommendations
            .FirstOrDefaultAsync(r => r.Id == recommendationId && r.UserId == userId, cancellationToken);

        if (recommendation == null) return false;

        var interaction = new RecommendationInteraction
        {
            RecommendationId = recommendationId,
            UserId = userId,
            InteractionType = interactionType,
            InteractedAtUtc = DateTime.UtcNow
        };

        _dbContext.RecommendationInteractions.Add(interaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<List<RecommendationResponseDto>> GenerateGoalAlignedRecommendationsAsync(Guid userId, int limit, CancellationToken cancellationToken)
    {
        var goals = await _dbContext.Goals
            .Where(g => g.UserId == userId && g.IsActive)
            .OrderBy(g => g.Priority)
            .ToListAsync(cancellationToken);

        var userTopics = await _dbContext.UserTopics
            .Include(ut => ut.Topic)
            .Where(ut => ut.UserId == userId)
            .OrderBy(ut => ut.MasteryScore) // find weakest areas first
            .ToListAsync(cancellationToken);

        var recommendations = new List<Recommendation>();

        // Goal-based recommendations
        foreach (var goal in goals)
        {
            var category = goal.Category;
            var goalTitle = goal.Title;

            if (category.Contains("Programming", StringComparison.OrdinalIgnoreCase) || goalTitle.Contains(".NET", StringComparison.OrdinalIgnoreCase) || goalTitle.Contains("developer", StringComparison.OrdinalIgnoreCase))
            {
                var weakDotnet = userTopics.FirstOrDefault(t => t.Topic?.Slug == "efcore" || t.Topic?.Name.Contains("EF Core") == true);
                var reason = weakDotnet != null && weakDotnet.MasteryScore < 60
                    ? "You've engaged with .NET concepts recently, but your EF Core mastery is developing. Strengthening relationship mapping will accelerate your interview preparation."
                    : $"Directly matches your active career goal: '{goal.Title}'.";

                recommendations.Add(new Recommendation
                {
                    UserId = userId,
                    GoalId = goal.Id,
                    Title = "Mastering EF Core 8 Relationship Mapping & Performance",
                    Description = "In-depth guide covering One-to-Many, Many-to-Many configurations, AsNoTracking optimizations, and eager vs explicit loading.",
                    Url = "https://learn.microsoft.com/en-us/ef/core/modeling/relationships",
                    ReasonDescription = reason,
                    SourceType = "Curated Learning",
                    RelevanceScore = 95.0,
                    Priority = 1,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
                });

                recommendations.Add(new Recommendation
                {
                    UserId = userId,
                    GoalId = goal.Id,
                    Title = "Clean Architecture in ASP.NET Core Web APIs",
                    Description = "Practical patterns for separating Domain, Application, and Infrastructure layers with Dependency Injection and MediatR/CQRS.",
                    Url = "https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures",
                    ReasonDescription = "Foundational architecture pattern for senior product-company engineering roles.",
                    SourceType = "Curated Learning",
                    RelevanceScore = 92.0,
                    Priority = 2,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
                });
            }
            else if (category.Contains("Finance", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new Recommendation
                {
                    UserId = userId,
                    GoalId = goal.Id,
                    Title = "Systematic Index Investing & Dollar-Cost Averaging",
                    Description = "Evidence-based strategies for long-term compound portfolio building with low-cost broad market ETFs.",
                    Url = "https://www.bogleheads.org/wiki/Bogleheads%C2%AE_investment_philosophy",
                    ReasonDescription = $"Aligned with your financial goal: '{goal.Title}'.",
                    SourceType = "Curated Learning",
                    RelevanceScore = 90.0,
                    Priority = 1,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
                });
            }
            else if (category.Contains("Fitness", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new Recommendation
                {
                    UserId = userId,
                    GoalId = goal.Id,
                    Title = "Evidence-Based Hypertrophy & Progressive Overload",
                    Description = "Scientific guidelines on weekly set volume, mechanical tension, and recovery periods for sustainable strength gains.",
                    Url = "https://www.ncbi.nlm.nih.gov/pmc/articles/PMC6950543/",
                    ReasonDescription = $"Personalized to support your fitness focus: '{goal.Title}'.",
                    SourceType = "Curated Learning",
                    RelevanceScore = 88.0,
                    Priority = 1,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
                });
            }
        }

        // If no goal was set yet, provide universal intentional consumption primer
        if (recommendations.Count == 0)
        {
            recommendations.Add(new Recommendation
            {
                UserId = userId,
                Title = "Transforming Passive Feeds Into Active Learning Systems",
                Description = "Techniques for curating algorithmic feeds, capturing technical insights on the fly, and spaced repetition recall.",
                Url = "https://scrollguardian.app/guides/intentional-consumption",
                ReasonDescription = "Helps build a baseline for intentional digital habits while you configure custom goals.",
                SourceType = "Curated Learning",
                RelevanceScore = 85.0,
                Priority = 1,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
            });
        }

        _dbContext.Recommendations.AddRange(recommendations);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return recommendations.Take(limit).Select(r => new RecommendationResponseDto
        {
            Id = r.Id,
            Title = r.Title,
            Description = r.Description,
            Url = r.Url,
            ReasonDescription = r.ReasonDescription,
            SourceType = r.SourceType,
            RelevanceScore = r.RelevanceScore,
            Priority = r.Priority
        }).ToList();
    }
}
