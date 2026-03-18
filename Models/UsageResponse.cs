using System.Globalization;
using System.Text.Json.Serialization;

namespace ClaudeUsageTray.Models;

/// <summary>
/// Root response from GET /api/oauth/usage.
/// The response is a flat object — no "data" wrapper.
/// </summary>
public sealed class UsageData
{
    [JsonPropertyName("five_hour")]
    public UsagePeriod? FiveHour { get; set; }

    [JsonPropertyName("seven_day")]
    public UsagePeriod? SevenDay { get; set; }
}

public sealed class UsagePeriod
{
    /// <summary>Utilization expressed as 0–100 (may be fractional, e.g. 43.5).</summary>
    [JsonPropertyName("utilization")]
    public double Utilization { get; set; }

    /// <summary>Raw resets_at string from the API, e.g. "2026-02-25T13:00:00.613722+00:00".</summary>
    [JsonPropertyName("resets_at")]
    public string ResetsAtRaw { get; set; } = "";

    [JsonIgnore]
    public DateTimeOffset ResetsAt => DateTimeOffset.TryParse(
        ResetsAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto)
        ? dto
        : DateTimeOffset.UtcNow;
}
