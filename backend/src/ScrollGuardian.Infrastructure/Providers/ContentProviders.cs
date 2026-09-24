using System.Text.RegularExpressions;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Enums;

namespace ScrollGuardian.Infrastructure.Providers;

public class BrowserExtensionContentProvider : IContentSourceProvider
{
    public ContentSourceProviderType SourceType => ContentSourceProviderType.BrowserExtension;

    public bool CanHandle(string url)
    {
        return !string.IsNullOrWhiteSpace(url);
    }

    public Task<ContentMetadataResult> ExtractMetadataAsync(string url, string? rawPayload = null, CancellationToken cancellationToken = default)
    {
        var result = new ContentMetadataResult
        {
            Url = url,
            PlatformContentId = GenerateIdFromUrl(url),
            Title = "Short-form Content",
            Creator = "Creator",
            Caption = "",
            Language = "en",
            DurationSeconds = 30
        };

        if (url.Contains("instagram.com/reel", StringComparison.OrdinalIgnoreCase) || url.Contains("instagram.com/reels", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(url, @"reel[s]?/([a-zA-Z0-9_-]+)");
            if (match.Success)
            {
                result.PlatformContentId = match.Groups[1].Value;
            }
        }
        else if (url.Contains("youtube.com/shorts/", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(url, @"shorts/([a-zA-Z0-9_-]+)");
            if (match.Success)
            {
                result.PlatformContentId = match.Groups[1].Value;
            }
        }

        return Task.FromResult(result);
    }

    private static string GenerateIdFromUrl(string url)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}

public class YouTubeShortsProvider : IContentSourceProvider
{
    public ContentSourceProviderType SourceType => ContentSourceProviderType.YouTubeShorts;

    public bool CanHandle(string url)
    {
        return !string.IsNullOrWhiteSpace(url) && (url.Contains("youtube.com/shorts", StringComparison.OrdinalIgnoreCase) || url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase));
    }

    public Task<ContentMetadataResult> ExtractMetadataAsync(string url, string? rawPayload = null, CancellationToken cancellationToken = default)
    {
        var match = Regex.Match(url, @"shorts/([a-zA-Z0-9_-]+)");
        var videoId = match.Success ? match.Groups[1].Value : "yt_" + Guid.NewGuid().ToString("N")[..8];

        return Task.FromResult(new ContentMetadataResult
        {
            PlatformContentId = videoId,
            Url = url,
            Title = $"YouTube Short ({videoId})",
            Creator = "YouTube Creator",
            DurationSeconds = 45,
            Language = "en"
        });
    }
}

public class InstagramReelsProvider : IContentSourceProvider
{
    public ContentSourceProviderType SourceType => ContentSourceProviderType.InstagramReels;

    public bool CanHandle(string url)
    {
        return !string.IsNullOrWhiteSpace(url) && url.Contains("instagram.com/reel", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ContentMetadataResult> ExtractMetadataAsync(string url, string? rawPayload = null, CancellationToken cancellationToken = default)
    {
        var match = Regex.Match(url, @"reel[s]?/([a-zA-Z0-9_-]+)");
        var reelId = match.Success ? match.Groups[1].Value : "ig_" + Guid.NewGuid().ToString("N")[..8];

        return Task.FromResult(new ContentMetadataResult
        {
            PlatformContentId = reelId,
            Url = url,
            Title = $"Instagram Reel ({reelId})",
            Creator = "Instagram Creator",
            DurationSeconds = 30,
            Language = "en"
        });
    }
}

public class ContentSourceProviderFactory
{
    private readonly IEnumerable<IContentSourceProvider> _providers;

    public ContentSourceProviderFactory(IEnumerable<IContentSourceProvider> providers)
    {
        _providers = providers;
    }

    public IContentSourceProvider GetProvider(string url, ContentSourceProviderType requestedType)
    {
        var match = _providers.FirstOrDefault(p => p.SourceType == requestedType && p.CanHandle(url));
        if (match != null) return match;

        var urlMatch = _providers.FirstOrDefault(p => p.CanHandle(url));
        if (urlMatch != null) return urlMatch;

        return _providers.First(p => p.SourceType == ContentSourceProviderType.BrowserExtension);
    }
}
