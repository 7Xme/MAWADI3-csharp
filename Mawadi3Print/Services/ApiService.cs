using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mawadi3Print.Models;

namespace Mawadi3Print.Services;

public partial class ApiService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Article> GenerateArticleAsync(
        string apiKey,
        string topic,
        string language,
        string model,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("ماكاينش مفتاح API. افتح الإعدادات (الترس) ودخل مفتاح Gemini.");

        var sanitizedModel = SanitizeModel(model);
        var langName = language == "fr" ? "الفرنسية" : "العربية";
        var systemPrompt = "أنت كاتب مقالات محترف. اكتب موضوعاً تعليمياً مناسباً للطلاب، مباشرةً بدون أي مقدمات أو عبارات ترحيبية. لا تضع أي شيء خارج المقال نفسه. لا تكتب \"هذا هو الموضوع\" أو \"إليك المقال\" أو أي جمل مشابهة. ابدأ فوراً بكتابة المقال.";
        var userPrompt = $"الموضوع: {topic}. اكتب مقالاً باللغة {langName}، طوله بين 350 و400 كلمة. لا تضع عنواناً للمقال (فقط النص). لا تستخدم أي تنسيق JSON أو Markdown. اكتب فقرات عادية مفصولة بسطرين فارغين. لا تضع أي شيء قبل المقال أو بعده.";

        var requestBody = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = userPrompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = 1500
            }
        };

        var json = JsonSerializer.Serialize(requestBody, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        Article? lastArticle = null;
        var maxAttempts = 2;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            Article article;
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(55));
                var response = await _http.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/{sanitizedModel}:generateContent?key={apiKey}",
                    content, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    throw await ParseHttpErrorAsync(response, errorBody);
                }

                var responseBody = await response.Content.ReadAsStringAsync(ct);
                article = ParseResponse(responseBody, topic, language);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("انتهى الوقت. تحقق من الاتصال بالإنترنت وعاود.");
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"خطأ فالشبكة. تحقق من الاتصال بالإنترنت. ({ex.Message})");
            }

            lastArticle = article;

            if (article.WordCount >= 350 || attempt >= maxAttempts)
                break;
        }

        if (lastArticle!.WordCount < 350)
        {
            // Return anyway, UI will warn
        }

        return lastArticle;
    }

    private static string SanitizeModel(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return "gemini-2.5-flash";
        return model.StartsWith("models/", StringComparison.OrdinalIgnoreCase)
            ? model[7..]
            : model;
    }

    private Article ParseResponse(string responseBody, string topic, string language)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var text = root
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("الموديل رجع محتوى فارغ.");

        // Check for blocked content
        if (root.TryGetProperty("promptFeedback", out var feedback) &&
            feedback.TryGetProperty("blockReason", out var blockReason))
        {
            throw new InvalidOperationException($"ماكاينش محتوى من Gemini. تم الحجب لأسباب أمنية: {blockReason}");
        }

        var raw = text;
        text = StripPrefixes(text.Trim());

        var (title, content) = ExtractTitle(text, topic);

        var wordCount = System.Text.RegularExpressions.Regex.Split(content, @"\s+")
            .Count(s => !string.IsNullOrWhiteSpace(s));

        return new Article
        {
            Title = title,
            Content = content,
            WordCount = wordCount,
            RawResponse = raw != content ? raw : null
        };
    }

    private static string StripPrefixes(string text)
    {
        // Arabic prefixes
        text = ArabicPrefixRegex().Replace(text, "");
        // French prefix
        text = FrenchPrefixRegex().Replace(text, "");
        // English prefixes
        text = EnglishPrefixRegex().Replace(text, "");
        // Generic "Article" prefix
        text = ArticlePrefixRegex().Replace(text, "");
        return text.Trim();
    }

    private static (string title, string content) ExtractTitle(string text, string fallbackTopic)
    {
        var lines = text.Split('\n', StringSplitOptions.None);
        var firstNonEmpty = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();

        if (firstNonEmpty != null && firstNonEmpty.Length < 80
            && !firstNonEmpty.EndsWith('.') && !firstNonEmpty.EndsWith('،') && !firstNonEmpty.EndsWith('؟'))
        {
            var title = firstNonEmpty;
            var content = string.Join("\n", lines.SkipWhile(l => l.Trim() == firstNonEmpty)).Trim();
            return (title, content);
        }

        return (fallbackTopic, text.Trim());
    }

    private static async Task<Exception> ParseHttpErrorAsync(HttpResponseMessage response, string errorBody)
    {
        var status = (int)response.StatusCode;
        var message = errorBody.ToLowerInvariant();

        if (status == 400 && message.Contains("key"))
            return new InvalidOperationException("مفتاح Gemini API غير صالح (400). تحقق من المفتاح فالإعدادات.");
        if (status == 403)
            return new InvalidOperationException("الوصول مرفوض (403). تحقق من مفتاح Gemini والرصيد/الحد.");
        if (status == 429)
            return new InvalidOperationException("تجاوزت الحد أو الرصيد (429). انتظر شوية وعاود المحاولة.");

        return new InvalidOperationException($"خطأ Gemini: {status} - {errorBody}");
    }

    [GeneratedRegex(@"^(هذا هو الموضوع|إليك المقال|إليك الموضوع|بالتأكيد، إليك|ها هو المقال|هذا المقال|المقال التالي)\s*[:;،\-]*\s*", RegexOptions.IgnoreCase | RegexOptions.RightToLeft)]
    private static partial Regex ArabicPrefixRegex();

    [GeneratedRegex(@"^Voici l['']article\s*[:;,\-]*\s*", RegexOptions.IgnoreCase)]
    private static partial Regex FrenchPrefixRegex();

    [GeneratedRegex(@"^Here is (?:your|the) article\s*[:;,\-]*\s*", RegexOptions.IgnoreCase)]
    private static partial Regex EnglishPrefixRegex();

    [GeneratedRegex(@"^Article\s*[:;,\-]*\s*", RegexOptions.IgnoreCase)]
    private static partial Regex ArticlePrefixRegex();
}