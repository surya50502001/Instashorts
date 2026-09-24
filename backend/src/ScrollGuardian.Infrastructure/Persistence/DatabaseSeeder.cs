using Microsoft.EntityFrameworkCore;
using ScrollGuardian.Domain.Entities;

namespace ScrollGuardian.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedReferenceTaxonomyAsync(ApplicationDbContext context)
    {
        if (await context.ContentCategories.AnyAsync())
        {
            return;
        }

        var categories = new List<ContentCategory>
        {
            new() { Id = Guid.NewGuid(), Name = "Programming", Slug = "programming", Description = "Software engineering, languages, frameworks, system design", DefaultColor = "#3b82f6" },
            new() { Id = Guid.NewGuid(), Name = "Education", Slug = "education", Description = "Academic subjects, tutorials, learning frameworks", DefaultColor = "#10b981" },
            new() { Id = Guid.NewGuid(), Name = "Science", Slug = "science", Description = "Physics, biology, space, neuroscience, tech research", DefaultColor = "#06b6d4" },
            new() { Id = Guid.NewGuid(), Name = "Finance", Slug = "finance", Description = "Investing, personal finance, economics, markets", DefaultColor = "#eab308" },
            new() { Id = Guid.NewGuid(), Name = "Fitness", Slug = "fitness", Description = "Strength training, nutrition, mobility, health habits", DefaultColor = "#ec4899" },
            new() { Id = Guid.NewGuid(), Name = "Business", Slug = "business", Description = "Product management, entrepreneurship, strategy", DefaultColor = "#8b5cf6" },
            new() { Id = Guid.NewGuid(), Name = "News", Slug = "news", Description = "Current events, tech news, global developments", DefaultColor = "#64748b" },
            new() { Id = Guid.NewGuid(), Name = "Entertainment", Slug = "entertainment", Description = "Casual entertainment, skits, stories", DefaultColor = "#f97316" },
            new() { Id = Guid.NewGuid(), Name = "Gaming", Slug = "gaming", Description = "Video games, esports, streams", DefaultColor = "#a855f7" },
            new() { Id = Guid.NewGuid(), Name = "Music", Slug = "music", Description = "Music performance, theory, production", DefaultColor = "#14b8a6" },
            new() { Id = Guid.NewGuid(), Name = "Comedy", Slug = "comedy", Description = "Humor, memes, stand-up", DefaultColor = "#f59e0b" },
            new() { Id = Guid.NewGuid(), Name = "Lifestyle", Slug = "lifestyle", Description = "Daily vlogs, travel, organization", DefaultColor = "#84cc16" },
            new() { Id = Guid.NewGuid(), Name = "Motivation", Slug = "motivation", Description = "Mindset, productivity habits, inspirational speeches", DefaultColor = "#ef4444" },
            new() { Id = Guid.NewGuid(), Name = "Other", Slug = "other", Description = "Unclassified or general content", DefaultColor = "#94a3b8" }
        };

        await context.ContentCategories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        // Add core topics under Programming & Education & Finance & Fitness
        var progCat = categories.First(c => c.Slug == "programming");
        var csharp = new ContentTopic { Id = Guid.NewGuid(), CategoryId = progCat.Id, Name = "C#", Slug = "csharp", Description = "C# programming language and idioms", KeywordsJson = "[\"c#\", \"csharp\", \"dotnet\", \".net\"]" };
        var dotnet = new ContentTopic { Id = Guid.NewGuid(), CategoryId = progCat.Id, Name = "ASP.NET Core", Slug = "aspnetcore", ParentTopicId = csharp.Id, Description = "ASP.NET Core web development and clean architecture", KeywordsJson = "[\"asp.net\", \"web api\", \"minimal api\", \"dependency injection\"]" };
        var efcore = new ContentTopic { Id = Guid.NewGuid(), CategoryId = progCat.Id, Name = "EF Core", Slug = "efcore", ParentTopicId = dotnet.Id, Description = "Entity Framework Core database mapping and migrations", KeywordsJson = "[\"ef core\", \"entity framework\", \"dbcontext\", \"linq\"]" };
        var docker = new ContentTopic { Id = Guid.NewGuid(), CategoryId = progCat.Id, Name = "Docker", Slug = "docker", Description = "Containerization and Docker images", KeywordsJson = "[\"docker\", \"container\", \"dockerfile\", \"docker-compose\"]" };
        var react = new ContentTopic { Id = Guid.NewGuid(), CategoryId = progCat.Id, Name = "React & TypeScript", Slug = "react-ts", Description = "Modern React web components and TypeScript", KeywordsJson = "[\"react\", \"typescript\", \"hooks\", \"tailwindcss\"]" };

        var finCat = categories.First(c => c.Slug == "finance");
        var investing = new ContentTopic { Id = Guid.NewGuid(), CategoryId = finCat.Id, Name = "Index Investing", Slug = "index-investing", Description = "Passive index funds and compound growth", KeywordsJson = "[\"etf\", \"index fund\", \"s&p 500\", \"compound interest\"]" };

        var fitCat = categories.First(c => c.Slug == "fitness");
        var strength = new ContentTopic { Id = Guid.NewGuid(), CategoryId = fitCat.Id, Name = "Strength Training", Slug = "strength-training", Description = "Progressive overload and resistance training", KeywordsJson = "[\"hypertrophy\", \"progressive overload\", \"squat\", \"deadlift\"]" };

        await context.ContentTopics.AddRangeAsync(new[] { csharp, dotnet, efcore, docker, react, investing, strength });
        await context.SaveChangesAsync();
    }
}
