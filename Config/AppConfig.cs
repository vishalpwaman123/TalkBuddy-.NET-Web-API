namespace TalkBuddy.API.Config;

/// <summary>
/// Strongly-typed representation of all application configuration values.
/// Bound from appsettings.json via IOptions&lt;AppConfig&gt;.
/// </summary>
public class AppConfig
{
    /// <summary>The Anthropic API key used to authenticate requests to Claude.</summary>
    public string AnthropicApiKey { get; set; } = string.Empty;

    /// <summary>SQL Server hostname or instance name (e.g. "localhost" or "server\\instance").</summary>
    public string DbServer { get; set; } = string.Empty;

    /// <summary>Name of the SQL Server database that contains the messages table.</summary>
    public string DbName { get; set; } = string.Empty;
}
