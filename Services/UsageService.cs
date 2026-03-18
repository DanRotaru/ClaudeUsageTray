using System.Net.Http.Headers;
using System.Text.Json;
using ClaudeUsageTray.Models;

namespace ClaudeUsageTray.Services;

public sealed class UsageService : IDisposable
{
    private const string UsageEndpoint = "https://api.anthropic.com/api/oauth/usage";
    private const string BetaHeader    = "oauth-2025-04-20";
    private const string UserAgent     = "claude-code/2.1.34";

    private readonly HttpClient _http = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<UsageData> GetUsageAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UsageEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("anthropic-beta", BetaHeader);
        request.Headers.UserAgent.ParseAdd(UserAgent);

        using var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Usage API returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        }

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<UsageData>(json, JsonOpts)
            ?? throw new InvalidOperationException(
                "Usage API response could not be deserialized.");
    }

    public void Dispose() => _http.Dispose();
}
