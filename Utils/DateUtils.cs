namespace ClaudeUsageTray.Utils;

public static class DateUtils
{
    /// <summary>
    /// Formats the time remaining until <paramref name="resetAt"/> as a human-readable string
    /// like "1d 2h 3m". Returns "now" if the timestamp is in the past.
    /// </summary>
    public static string FormatTimeUntil(DateTimeOffset resetAt)
    {
        var remaining = resetAt - DateTimeOffset.UtcNow;

        if (remaining <= TimeSpan.Zero)
            return "now";

        var days    = (int)remaining.TotalDays;
        var hours   = remaining.Hours;
        var minutes = remaining.Minutes;

        var parts = new List<string>(3);
        if (days    > 0) parts.Add($"{days}d");
        if (hours   > 0) parts.Add($"{hours}h");
        if (minutes > 0 || parts.Count == 0) parts.Add($"{minutes}m");

        return string.Join(" ", parts);
    }
}
