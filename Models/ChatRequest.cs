namespace TalkBuddy.API.Models;

/// <summary>Inbound payload for the POST /chat endpoint.</summary>
public record ChatRequest(string UserId, string SessionId, string Message);
