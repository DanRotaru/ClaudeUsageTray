using System.Text.Json;
using ClaudeUsageTray.Models;

namespace ClaudeUsageTray.Services;

public sealed class CredentialsService
{
    /// <summary>
    /// Reads the Claude credentials file for the current OS user and returns the OAuth access token.
    /// Path: C:\Users\{username}\.claude\.credentials.json
    /// </summary>
    public string GetAccessToken()
    {
        var username = Environment.UserName;
        var path = Path.Combine(
            @"C:\Users", username, ".claude", ".credentials.json");

        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Credentials file not found at: {path}\n" +
                "Make sure Claude Code is installed and you have logged in.", path);

        var json = File.ReadAllText(path);

        var credentials = JsonSerializer.Deserialize<ClaudeCredentials>(json)
            ?? throw new InvalidOperationException("Failed to parse credentials file.");

        var token = credentials.ClaudeAiOauth?.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "Access token is missing from credentials file.");

        return token;
    }
}
