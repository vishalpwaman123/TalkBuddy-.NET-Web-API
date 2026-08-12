namespace TalkBuddy.API.Models;

/// <summary>Represents a single row from the messages table — a role/content pair.</summary>
public record Message(string Role, string Content, string Timestamp);
