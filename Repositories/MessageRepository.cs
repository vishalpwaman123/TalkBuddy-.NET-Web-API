using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TalkBuddy.API.Config;
using TalkBuddy.API.Models;

namespace TalkBuddy.API.Repositories;

/// <summary>
/// Handles all SQL Server interactions for the messages table.
/// Uses Windows Authentication (Trusted_Connection=True) — no username or password required.
/// </summary>
public class MessageRepository
{
    private readonly AppConfig _config;
    private readonly ILogger<MessageRepository> _logger;

    public MessageRepository(IOptions<AppConfig> config, ILogger<MessageRepository> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    private string ConnectionString =>
        $"Server={_config.DbServer};Database={_config.DbName};Trusted_Connection=True;TrustServerCertificate=True;";

    /// <summary>
    /// Inserts a new message row into the messages table with the given user, session, role, and content.
    /// </summary>
    public async Task SaveMessageAsync(string userId, string sessionId, string role, string content)
    {
        const string sql = """
            INSERT INTO messages (user_id, session_id, role, content)
            VALUES (@UserId, @SessionId, @Role, @Content)
            """;

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@SessionId", sessionId);
        command.Parameters.AddWithValue("@Role", role);
        command.Parameters.AddWithValue("@Content", content);

        await command.ExecuteNonQueryAsync();
        _logger.LogInformation("Saved {Role} message for user {UserId} in session {SessionId}", role, userId, sessionId);
    }

    /// <summary>
    /// Returns all messages for the given user, ordered by timestamp ascending (oldest first).
    /// When limit is provided, returns only the most recent N messages, still in ascending order.
    /// </summary>
    public async Task<List<Message>> GetHistoryAsync(string userId, int? limit = null)
    {
        string sql = limit.HasValue
            ? $"SELECT TOP {limit.Value} role, content, timestamp FROM messages WHERE user_id = @UserId ORDER BY timestamp DESC"
            : "SELECT role, content, timestamp FROM messages WHERE user_id = @UserId ORDER BY timestamp DESC";

        var rows = new List<Message>();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new Message(reader.GetString(0), reader.GetString(1), reader.GetDateTime(2).ToString("o")));
        }

        // Results came back DESC (newest first); reverse to restore chronological order.
        rows.Reverse();
        _logger.LogInformation("Retrieved {Count} messages for user {UserId} (limit={Limit})", rows.Count, userId, limit?.ToString() ?? "none");
        return rows;
    }

    /// <summary>
    /// Returns all messages for the given user ordered by timestamp ascending (oldest first).
    /// Used to display full conversation history on the frontend.
    /// </summary>
    public async Task<List<Message>> GetHistoryByUserIdAsync(string userId)
    {
        const string sql = "SELECT role, content, timestamp FROM messages WHERE user_id = @UserId ORDER BY timestamp ASC";

        var rows = new List<Message>();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new Message(reader.GetString(0), reader.GetString(1), reader.GetDateTime(2).ToString("o")));
        }

        _logger.LogInformation("Retrieved full history ({Count} messages) for user {UserId}", rows.Count, userId);
        return rows;
    }

    /// <summary>
    /// Returns the most recent 20 messages for the given user in chronological order (oldest of the 20 first).
    /// Used to build the memory context that is injected into the system prompt.
    /// </summary>
    public async Task<List<Message>> GetRecentMessagesAsync(string userId, int limit = 20)
    {
        return await GetHistoryAsync(userId, limit);
    }
}
