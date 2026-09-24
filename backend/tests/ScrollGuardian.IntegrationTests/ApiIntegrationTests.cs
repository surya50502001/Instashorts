using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Entities;
using ScrollGuardian.Domain.Enums;
using ScrollGuardian.Infrastructure.Persistence;
using Xunit;

namespace ScrollGuardian.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // Remove existing db context
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });
    }
}

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health");
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteE2EUserJourney_Register_Onboard_Ingest_Dashboard_Quiz_Export()
    {
        // 1. Register
        var email = $"user_{Guid.NewGuid():N}@scrollguardian.app";
        var registerDto = new RegisterRequestDto
        {
            Email = email,
            Password = "Password123!",
            FullName = "Jane Doe"
        };

        var regRes = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        regRes.IsSuccessStatusCode.Should().BeTrue();
        var authData = await regRes.Content.ReadFromJsonAsync<AuthResponseDto>();
        authData.Should().NotBeNull();
        authData!.AccessToken.Should().NotBeNullOrEmpty();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData.AccessToken);

        // 2. Submit Onboarding
        var onboardDto = new OnboardingRequestDto
        {
            SelectedCategories = new List<string> { "Programming", "Career" },
            PrimaryGoalText = "Master .NET 8 Web APIs and Clean Architecture",
            EstimatedDailyMinutes = 45,
            Aggressiveness = AggressivenessLevel.Balanced,
            ConsentToTelemetry = true,
            ConsentToAiAnalysis = true
        };

        var onboardRes = await client.PostAsJsonAsync("/api/onboarding", onboardDto);
        onboardRes.IsSuccessStatusCode.Should().BeTrue();

        // 3. Ingest Content Event
        var ingestDto = new IngestContentEventDto
        {
            Url = "https://www.youtube.com/shorts/testDocker123",
            Title = "Docker containerization in 60 seconds",
            Creator = "TechDev",
            Caption = "Essential docker tips for .NET developers",
            TimeSpentSeconds = 45,
            CompletionPercentage = 100,
            UserAction = ContentUserAction.WatchedFull,
            SourceProvider = ContentSourceProviderType.YouTubeShorts
        };

        var ingestRes = await client.PostAsJsonAsync("/api/content/events", ingestDto);
        ingestRes.IsSuccessStatusCode.Should().BeTrue();

        // 4. Fetch Dashboard Summary
        var dashRes = await client.GetAsync("/api/analytics/dashboard");
        dashRes.IsSuccessStatusCode.Should().BeTrue();
        var dashboard = await dashRes.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        dashboard.Should().NotBeNull();
        dashboard!.ItemsConsumedCount.Should().BeGreaterThanOrEqualTo(1);

        // 5. Fetch Recommendations
        var recsRes = await client.GetAsync("/api/recommendations");
        recsRes.IsSuccessStatusCode.Should().BeTrue();
        var recs = await recsRes.Content.ReadFromJsonAsync<List<RecommendationResponseDto>>();
        recs.Should().NotBeEmpty();

        // 6. Test Privacy Export
        var exportRes = await client.GetAsync("/api/privacy/export");
        exportRes.IsSuccessStatusCode.Should().BeTrue();
        var export = await exportRes.Content.ReadFromJsonAsync<UserDataExportDto>();
        export.Should().NotBeNull();
        export!.Profile.Email.Should().Be(email);
        export.Goals.Should().NotBeEmpty();
    }
}
