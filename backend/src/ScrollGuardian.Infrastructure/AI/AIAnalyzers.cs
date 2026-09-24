using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Enums;

namespace ScrollGuardian.Infrastructure.AI;

public class RuleBasedContentAnalyzer : IAIContentAnalyzer
{
    public string ProviderName => "RuleBasedHeuristic";

    private static readonly Dictionary<string, string[]> CategoryKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Programming"] = new[] { "c#", ".net", "dotnet", "csharp", "react", "typescript", "javascript", "python", "docker", "kubernetes", "sql", "api", "database", "git", "algorithm", "architecture", "microservices", "frontend", "backend", "developer", "coding", "software", "ef core", "entity framework" },
        ["Science"] = new[] { "physics", "quantum", "biology", "space", "nasa", "james webb", "neuroscience", "chemistry", "astronomy", "scientific", "experiment", "evolution", "dna" },
        ["Finance"] = new[] { "investing", "stocks", "etf", "s&p 500", "compound interest", "budget", "roth ira", "real estate", "crypto", "dividend", "passive income", "inflation", "economics", "index fund" },
        ["Fitness"] = new[] { "workout", "gym", "hypertrophy", "progressive overload", "protein", "deadlift", "squat", "bench press", "calisthenics", "mobility", "stretching", "cardio", "nutrition", "bodybuilding" },
        ["Business"] = new[] { "startup", "saas", "entrepreneur", "marketing", "sales", "product manager", "revenue", "b2b", "scale", "pricing", "leadership", "management" },
        ["Education"] = new[] { "tutorial", "how to", "explained", "course", "lesson", "lecture", "study", "history", "philosophy", "languages", "learn" },
        ["News"] = new[] { "breaking", "news", "report", "update", "politics", "election", "war", "economy", "market update", "announcement" },
        ["Comedy"] = new[] { "funny", "meme", "comedy", "prank", "sketch", "hilarious", "joke", "lol", "laugh", "standup" },
        ["Gaming"] = new[] { "gameplay", "gaming", "fortnite", "minecraft", "gta", "playstation", "xbox", "esports", "speedrun", "roblox" },
        ["Music"] = new[] { "guitar", "piano", "song", "cover", "singing", "beat", "producer", "vocal", "chords", "music production" },
        ["Motivation"] = new[] { "mindset", "discipline", "quote", "stoic", "motivation", "success", "grind", "focus", "habits" },
        ["Entertainment"] = new[] { "dance", "trend", "challenge", "vlog", "celebrity", "drama", "reaction", "shorts", "reels" }
    };

    public Task<AIAnalysisResult> AnalyzeContentAsync(
        string title,
        string creator,
        string caption,
        string transcript,
        List<string> userGoalKeywords,
        CancellationToken cancellationToken = default)
    {
        var fullText = $"{title} {caption} {transcript}".ToLowerInvariant();

        // 1. Determine Category & Primary Topic
        string bestCategory = "Other";
        int maxMatches = 0;
        string matchedTopic = "General";

        foreach (var (cat, keywords) in CategoryKeywords)
        {
            int matches = 0;
            string firstMatched = string.Empty;
            foreach (var kw in keywords)
            {
                if (fullText.Contains(kw))
                {
                    matches++;
                    if (string.IsNullOrEmpty(firstMatched))
                    {
                        firstMatched = kw;
                    }
                }
            }

            if (matches > maxMatches)
            {
                maxMatches = matches;
                bestCategory = cat;
                matchedTopic = string.IsNullOrEmpty(firstMatched) ? cat : char.ToUpper(firstMatched[0]) + firstMatched[1..];
            }
        }

        // 2. Estimate Informational Depth (0 - 100)
        double informationDepth = 15.0;
        if (bestCategory is "Programming" or "Science" or "Finance" or "Education" or "Business")
        {
            informationDepth = 65.0 + Math.Min(30.0, maxMatches * 5.0);
            if (fullText.Length > 200 || !string.IsNullOrWhiteSpace(transcript))
            {
                informationDepth += 5.0;
            }
        }
        else if (bestCategory is "Fitness" or "Music" or "Motivation")
        {
            informationDepth = 45.0 + Math.Min(25.0, maxMatches * 4.0);
        }
        else if (bestCategory is "Entertainment" or "Comedy" or "Gaming")
        {
            informationDepth = 10.0 + Math.Min(20.0, maxMatches * 2.0);
        }
        informationDepth = Math.Clamp(informationDepth, 5.0, 95.0);

        // 3. Estimate Educational Value (0 - 100)
        double educationalValue = (bestCategory is "Programming" or "Education" or "Science" or "Finance" or "Fitness" or "Business")
            ? Math.Clamp(informationDepth * 0.9 + 10.0, 40.0, 95.0)
            : Math.Clamp(informationDepth * 0.4, 5.0, 35.0);

        // 4. Calculate Goal Relevance (0 - 100)
        double goalRelevance = 10.0;
        if (userGoalKeywords.Count > 0)
        {
            var allGoalTokens = userGoalKeywords
                .SelectMany(g => g.Split(new[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(t => t.Length > 1)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            int goalHits = allGoalTokens.Count(kw => fullText.Contains(kw.ToLowerInvariant()));
            if (goalHits > 0)
            {
                goalRelevance = Math.Min(95.0, 50.0 + (goalHits * 15.0));
            }
            else if (bestCategory is "Programming" or "Science" or "Education")
            {
                goalRelevance = 30.0;
            }
        }
        else if (bestCategory is "Programming" or "Education" or "Science" or "Finance")
        {
            goalRelevance = 50.0;
        }

        // 5. Repetition Score (0 - 100)
        double repetitionScore = (bestCategory is "Comedy" or "Entertainment" or "Gaming" or "Motivation") ? 65.0 : 25.0;

        // 6. Summary and Key Takeaways
        var summary = string.IsNullOrWhiteSpace(caption)
            ? $"Overview of {matchedTopic} in the context of {bestCategory} content."
            : (caption.Length > 160 ? caption[..157] + "..." : caption);

        var takeaways = new List<string>
        {
            $"Core insight on {matchedTopic} within {bestCategory}.",
            $"Practical awareness regarding {(!string.IsNullOrEmpty(creator) ? creator + "'s" : "the")} presented concepts."
        };

        // 7. Dynamic Quiz Generation for Educational / Technical Content
        var generatedQuestions = new List<AIKnowledgeQuestionGenerated>();
        if (educationalValue >= 45.0)
        {
            generatedQuestions.Add(GenerateContextualQuestion(matchedTopic, bestCategory));
        }

        var result = new AIAnalysisResult
        {
            Category = bestCategory,
            PrimaryTopic = matchedTopic,
            SecondaryTopics = new List<string> { bestCategory, matchedTopic },
            InformationDepth = informationDepth,
            EducationalValue = educationalValue,
            Novelty = 70.0,
            GoalRelevance = goalRelevance,
            RepetitionScore = repetitionScore,
            PracticalValue = Math.Round(educationalValue * 0.85, 1),
            SourceConfidence = 85.0,
            Summary = summary,
            KeyTakeaways = takeaways,
            ClaimsRequiringVerification = new List<string>(),
            GeneratedQuestions = generatedQuestions,
            ModelUsed = ProviderName,
            PromptTokens = 120,
            CompletionTokens = 85,
            EstimatedCostUsd = 0.000m
        };

        return Task.FromResult(result);
    }

    private static AIKnowledgeQuestionGenerated GenerateContextualQuestion(string topic, string category)
    {
        if (topic.Contains("c#", StringComparison.OrdinalIgnoreCase) || topic.Contains("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return new AIKnowledgeQuestionGenerated
            {
                QuestionText = "In .NET and C#, which memory management concept is responsible for reclaiming unused managed heap objects?",
                Options = new List<string> { "Garbage Collector (GC)", "Manual free() pointer handler", "Reference Counter Hook", "Stack Pointer Resolver" },
                CorrectOptionIndex = 0,
                Explanation = "The .NET Garbage Collector automatically handles memory allocation and deallocation for managed heap objects.",
                Difficulty = KnowledgeQuestionDifficulty.Medium
            };
        }
        else if (topic.Contains("docker", StringComparison.OrdinalIgnoreCase) || topic.Contains("container", StringComparison.OrdinalIgnoreCase))
        {
            return new AIKnowledgeQuestionGenerated
            {
                QuestionText = "What is the primary operational advantage of containerizing an application with Docker?",
                Options = new List<string> { "Complete hardware-level hypervisor virtualization", "Consistent runtime environment across development and production", "Guaranteed 10x CPU overclocking", "Removal of all networking layers" },
                CorrectOptionIndex = 1,
                Explanation = "Docker encapsulates dependencies and code to ensure identical execution behavior across diverse host machines.",
                Difficulty = KnowledgeQuestionDifficulty.Easy
            };
        }
        else if (topic.Contains("ef", StringComparison.OrdinalIgnoreCase) || topic.Contains("entity", StringComparison.OrdinalIgnoreCase))
        {
            return new AIKnowledgeQuestionGenerated
            {
                QuestionText = "In Entity Framework Core, how can you optimize read-only queries to prevent change-tracking overhead?",
                Options = new List<string> { "Use .AsNoTracking()", "Call .SaveChanges() before querying", "Disable DB Connection pooling", "Cast the DbSet to an Array" },
                CorrectOptionIndex = 0,
                Explanation = "The .AsNoTracking() extension method tells EF Core not to track entities in the change tracker, significantly reducing memory and CPU usage for read queries.",
                Difficulty = KnowledgeQuestionDifficulty.Medium
            };
        }
        else if (category.Equals("Finance", StringComparison.OrdinalIgnoreCase))
        {
            return new AIKnowledgeQuestionGenerated
            {
                QuestionText = $"When discussing {topic}, what is the main benefit of broad-market index fund investing over single-stock stock picking?",
                Options = new List<string> { "Built-in diversification across hundreds of companies", "Guaranteed daily positive returns", "Zero tax obligations forever", "Immediate physical gold settlement" },
                CorrectOptionIndex = 0,
                Explanation = "Broad index funds spread risk across entire markets, capturing market-wide compound growth without idiosyncratic single-company risk.",
                Difficulty = KnowledgeQuestionDifficulty.Easy
            };
        }
        else if (category.Equals("Fitness", StringComparison.OrdinalIgnoreCase))
        {
            return new AIKnowledgeQuestionGenerated
            {
                QuestionText = $"What is the fundamental principle of 'Progressive Overload' in {topic}?",
                Options = new List<string> { "Gradually increasing the stress (weight, reps, or volume) placed on the body over time", "Working out to extreme muscle failure every single set", "Eliminating all rest days between workouts", "Only lifting identical weights each month" },
                CorrectOptionIndex = 0,
                Explanation = "Progressive overload forces adaptation and muscle growth by incrementally increasing workload over training cycles.",
                Difficulty = KnowledgeQuestionDifficulty.Easy
            };
        }

        return new AIKnowledgeQuestionGenerated
        {
            QuestionText = $"Based on this content about {topic}, what is the key factor for mastering this concept?",
            Options = new List<string> { "Active application and spaced recall", "Passive rapid scrolling without review", "Memorizing without understanding principles", "Avoiding practical exercises" },
            CorrectOptionIndex = 0,
            Explanation = "Intentional consumption and active application ensure meaningful retention of newly learned concepts.",
            Difficulty = KnowledgeQuestionDifficulty.Easy
        };
    }
}

public class GeminiContentAnalyzer : IAIContentAnalyzer
{
    public string ProviderName => "GoogleGemini";
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiContentAnalyzer> _logger;
    private readonly RuleBasedContentAnalyzer _fallbackAnalyzer = new();

    public GeminiContentAnalyzer(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiContentAnalyzer> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AIAnalysisResult> AnalyzeContentAsync(
        string title,
        string creator,
        string caption,
        string transcript,
        List<string> userGoalKeywords,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["AI:GeminiApiKey"] ?? _configuration["GEMINI_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogInformation("Gemini API key is not configured. Falling back to RuleBasedContentAnalyzer.");
            return await _fallbackAnalyzer.AnalyzeContentAsync(title, creator, caption, transcript, userGoalKeywords, cancellationToken);
        }

        try
        {
            var model = _configuration["AI:Model"] ?? "gemini-1.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var prompt = $$"""
            Analyze the following short-form social video content metadata.
            Return a STRICT JSON object only. Do not include markdown code block backticks.
            Content Title: {{title}}
            Creator: {{creator}}
            Caption: {{caption}}
            Transcript/Text: {{transcript}}
            User Goal Keywords: {{string.Join(", ", userGoalKeywords)}}

            Schema:
            {
              "category": "Programming" | "Education" | "Science" | "Finance" | "Fitness" | "Business" | "News" | "Entertainment" | "Gaming" | "Music" | "Comedy" | "Lifestyle" | "Motivation" | "Other",
              "primaryTopic": "string",
              "secondaryTopics": ["string"],
              "informationDepth": number (0 to 100),
              "educationalValue": number (0 to 100),
              "novelty": number (0 to 100),
              "goalRelevance": number (0 to 100),
              "repetitionScore": number (0 to 100),
              "practicalValue": number (0 to 100),
              "sourceConfidence": number (0 to 100),
              "summary": "string (1-2 sentences)",
              "keyTakeaways": ["string"],
              "claimsRequiringVerification": ["string"],
              "question": {
                 "questionText": "string",
                 "options": ["string", "string", "string", "string"],
                 "correctOptionIndex": number (0-3),
                 "explanation": "string",
                 "difficulty": "Easy" | "Medium" | "Hard"
              }
            }
            """;

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini API call failed with status {StatusCode}. Using fallback.", response.StatusCode);
                return await _fallbackAnalyzer.AnalyzeContentAsync(title, creator, caption, transcript, userGoalKeywords, cancellationToken);
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseString);
            var textElement = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(textElement))
            {
                return await _fallbackAnalyzer.AnalyzeContentAsync(title, creator, caption, transcript, userGoalKeywords, cancellationToken);
            }

            // Clean up possible formatting
            var cleanJson = textElement.Trim();
            if (cleanJson.StartsWith("```json")) cleanJson = cleanJson[7..];
            if (cleanJson.StartsWith("```")) cleanJson = cleanJson[3..];
            if (cleanJson.EndsWith("```")) cleanJson = cleanJson[..^3];
            cleanJson = cleanJson.Trim();

            using var parsed = JsonDocument.Parse(cleanJson);
            var root = parsed.RootElement;

            var result = new AIAnalysisResult
            {
                Category = root.TryGetProperty("category", out var cat) ? cat.GetString() ?? "Other" : "Other",
                PrimaryTopic = root.TryGetProperty("primaryTopic", out var pt) ? pt.GetString() ?? "General" : "General",
                InformationDepth = root.TryGetProperty("informationDepth", out var id) ? id.GetDouble() : 50.0,
                EducationalValue = root.TryGetProperty("educationalValue", out var ev) ? ev.GetDouble() : 50.0,
                Novelty = root.TryGetProperty("novelty", out var nov) ? nov.GetDouble() : 70.0,
                GoalRelevance = root.TryGetProperty("goalRelevance", out var gr) ? gr.GetDouble() : 20.0,
                RepetitionScore = root.TryGetProperty("repetitionScore", out var rep) ? rep.GetDouble() : 30.0,
                PracticalValue = root.TryGetProperty("practicalValue", out var pv) ? pv.GetDouble() : 40.0,
                SourceConfidence = root.TryGetProperty("sourceConfidence", out var sc) ? sc.GetDouble() : 80.0,
                Summary = root.TryGetProperty("summary", out var sm) ? sm.GetString() ?? "" : "",
                ModelUsed = "gemini-1.5-flash",
                PromptTokens = 450,
                CompletionTokens = 220,
                EstimatedCostUsd = 0.00015m
            };

            if (root.TryGetProperty("secondaryTopics", out var st) && st.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in st.EnumerateArray()) result.SecondaryTopics.Add(el.GetString() ?? "");
            }
            if (root.TryGetProperty("keyTakeaways", out var kt) && kt.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in kt.EnumerateArray()) result.KeyTakeaways.Add(el.GetString() ?? "");
            }
            if (root.TryGetProperty("claimsRequiringVerification", out var cr) && cr.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in cr.EnumerateArray()) result.ClaimsRequiringVerification.Add(el.GetString() ?? "");
            }

            if (root.TryGetProperty("question", out var q) && q.ValueKind == JsonValueKind.Object)
            {
                var qObj = new AIKnowledgeQuestionGenerated
                {
                    QuestionText = q.TryGetProperty("questionText", out var qt) ? qt.GetString() ?? "" : "",
                    CorrectOptionIndex = q.TryGetProperty("correctOptionIndex", out var coi) ? coi.GetInt32() : 0,
                    Explanation = q.TryGetProperty("explanation", out var exp) ? exp.GetString() ?? "" : "",
                    Difficulty = KnowledgeQuestionDifficulty.Medium
                };
                if (q.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var opt in opts.EnumerateArray()) qObj.Options.Add(opt.GetString() ?? "");
                }
                if (!string.IsNullOrEmpty(qObj.QuestionText) && qObj.Options.Count >= 2)
                {
                    result.GeneratedQuestions.Add(qObj);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini analysis error, falling back to rule-based engine.");
            return await _fallbackAnalyzer.AnalyzeContentAsync(title, creator, caption, transcript, userGoalKeywords, cancellationToken);
        }
    }
}
