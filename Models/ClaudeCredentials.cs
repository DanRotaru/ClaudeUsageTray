using System.Text.Json.Serialization;

namespace ClaudeUsageTray.Models;

public sealed class ClaudeCredentials
{
    [JsonPropertyName("claudeAiOauth")]
    public ClaudeAiOauth? ClaudeAiOauth { get; set; }

    [JsonPropertyName("organizationUuid")]
    public string? OrganizationUuid { get; set; }
}

public sealed class ClaudeAiOauth
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expiresAt")]
    public long ExpiresAt { get; set; }

    [JsonPropertyName("subscriptionType")]
    public string? SubscriptionType { get; set; }

    [JsonPropertyName("rateLimitTier")]
    public string? RateLimitTier { get; set; }
}
