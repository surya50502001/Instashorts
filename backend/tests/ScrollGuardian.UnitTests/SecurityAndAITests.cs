using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Entities;
using ScrollGuardian.Domain.Enums;
using ScrollGuardian.Infrastructure.AI;
using ScrollGuardian.Infrastructure.Security;
using Xunit;

namespace ScrollGuardian.UnitTests;

public class SecurityAndAuthTests
{
    private readonly PasswordHasher _passwordHasher = new();
    private readonly JwtTokenService _jwtTokenService;

    public SecurityAndAuthTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "ScrollGuardian_SuperSecretKey_ProductionGrade_2026_Minimum32Characters!",
                ["Jwt:Issuer"] = "ScrollGuardianApi",
                ["Jwt:Audience"] = "ScrollGuardianClient",
                ["Jwt:ExpiryMinutes"] = "60",
                ["Jwt:RefreshTokenDays"] = "30"
            })
            .Build();

        _jwtTokenService = new JwtTokenService(config);
    }

    [Fact]
    public void HashPassword_ShouldGenerateDifferentHashesForSamePassword()
    {
        var password = "SecurePassword123!";
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        hash1.Should().NotBeNullOrEmpty();
        hash2.Should().NotBeNullOrEmpty();
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrueForCorrectPassword()
    {
        var password = "CorrectPassword123!";
        var hash = _passwordHasher.HashPassword(password);

        var isValid = _passwordHasher.VerifyPassword(password, hash);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalseForWrongPassword()
    {
        var password = "CorrectPassword123!";
        var hash = _passwordHasher.HashPassword(password);

        var isValid = _passwordHasher.VerifyPassword("WrongPassword123!", hash);
        isValid.Should().BeFalse();
    }

    [Fact]
    public void JwtToken_ShouldGenerateAndValidateSuccessfully()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@scrollguardian.app",
            FullName = "Test User",
            Role = UserRole.User,
            Plan = SubscriptionPlan.Pro
        };

        var token = _jwtTokenService.GenerateAccessToken(user);
        token.Should().NotBeNullOrEmpty();

        var isValid = _jwtTokenService.ValidateAccessToken(token, out var extractedUserId);
        isValid.Should().BeTrue();
        extractedUserId.Should().Be(user.Id);
    }

    [Fact]
    public void RefreshToken_ShouldHaveCorrectExpirationAndActiveStatus()
    {
        var userId = Guid.NewGuid();
        var refreshToken = _jwtTokenService.GenerateRefreshToken(userId, "127.0.0.1");

        refreshToken.UserId.Should().Be(userId);
        refreshToken.Token.Should().NotBeNullOrEmpty();
        refreshToken.IsActive.Should().BeTrue();
        refreshToken.IsExpired.Should().BeFalse();
        refreshToken.IsRevoked.Should().BeFalse();
        refreshToken.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow.AddDays(25));
    }
}

public class AIContentAnalyzerTests
{
    private readonly RuleBasedContentAnalyzer _analyzer = new();

    [Fact]
    public async Task AnalyzeContent_ShouldClassifyProgrammingAndGenerateQuiz()
    {
        var title = "Building Microservices with ASP.NET Core and EF Core";
        var creator = "Nick Chapsas";
        var caption = "Learn how to configure EF Core DbContext and clean architecture in .NET 8";
        var transcript = "In this video we explore Entity Framework Core and database optimization with AsNoTracking.";
        var userGoals = new List<string> { ".NET developer", "C#" };

        var result = await _analyzer.AnalyzeContentAsync(title, creator, caption, transcript, userGoals);

        result.Category.Should().Be("Programming");
        result.PrimaryTopic.Should().NotBeEmpty();
        result.InformationDepth.Should().BeGreaterThan(60.0);
        result.EducationalValue.Should().BeGreaterThan(60.0);
        result.GoalRelevance.Should().BeGreaterThan(50.0);
        result.GeneratedQuestions.Should().NotBeEmpty();
        result.GeneratedQuestions[0].Options.Count.Should().Be(4);
    }

    [Fact]
    public async Task AnalyzeContent_ShouldClassifyEntertainmentWithLowEducationalScore()
    {
        var title = "Hilarious prank compilation 2026 lol";
        var creator = "MemeLord";
        var caption = "Try not to laugh challenge funny moments";
        var transcript = "Wait till the end, this comedy skit is crazy";
        var userGoals = new List<string> { "Programming" };

        var result = await _analyzer.AnalyzeContentAsync(title, creator, caption, transcript, userGoals);

        result.Category.Should().BeOneOf("Comedy", "Entertainment");
        result.EducationalValue.Should().BeLessThan(40.0);
        result.GoalRelevance.Should().BeLessThan(35.0);
        result.RepetitionScore.Should().BeGreaterThan(50.0);
        result.GeneratedQuestions.Should().BeEmpty();
    }
}
