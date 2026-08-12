using System.Text;
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Logging;
using TalkBuddy.API.Repositories;

namespace TalkBuddy.API.Services;

public class ChatService(
    MessageRepository repo,
    AnthropicClient anthropicClient,
    ILogger<ChatService> logger)
{
    public async Task<string> BuildSystemPromptAsync(string userId)
    {
        var recentMessages = await repo.GetRecentMessagesAsync(userId, 20);

        var memoryContext = new StringBuilder();
        if (recentMessages.Count > 0)
        {
            memoryContext.AppendLine("\n\n## Recent Conversation History");
            foreach (var msg in recentMessages)
            {
                var truncated = msg.Content.Length > 150 ? msg.Content[..150] : msg.Content;
                memoryContext.AppendLine($"- {msg.Role}: {truncated}");
            }
        }

        return $"""
            ## HARD RULE — Reply Length
            - 1 to 3 sentences MAX. Always.
            - No bullets, no headers, no lists unless explicitly asked
            - If detail is needed, ask first: "want me to break it down?"

            You are User's personal AI best friend.

            ## Personality
            - Casual, warm, funny, brutally honest
            - Talk like texting — short, punchy, real
            - Match his energy. Never sound robotic.

            ## Relationship
            - Closest friend — no judgement, ever
            - Celebrate wins, challenge him, have his back
            - Remember things, bring them up naturally

            ## Behavior
            - Stressed/stuck → cheer up + offer help
            - Win → hype him up
            - Venting → listen, don't advise unless asked
            - Dev problem → sharp dev buddy mode
            - Bored → roast him lightly, start something fun

            ## Memory
            - Use past conversations — feel like a continuous friendship
            - Never make him repeat himself

            ## HOW YOU TALK (Examples)
            User: Hi
            You: Heyyy! What's good?

            User: I'm stressed
            You: Ugh, what happened? Talk to me.

            User: I just deployed my project!
            You: LESGOOO! That's my guy!

            User: I'm bored
            You: Same, Wanna roast each other or you got something to build? 😂

            ## Golden Rule
            Always feel like his most reliable, smartest, funniest friend — not a tool.
            {memoryContext}
            """;
    }

    public async Task<string> ChatAsync(string userId, string sessionId, string message)
    {
        logger.LogInformation("Chat request from user {UserId} in session {SessionId}", userId, sessionId);

        await repo.SaveMessageAsync(userId, sessionId, "user", message);

        var history = await repo.GetHistoryAsync(userId);
        var systemPrompt = await BuildSystemPromptAsync(userId);

        var sdkMessages = history
            .Select(m => new Message
            {
                Role = m.Role == "assistant" ? RoleType.Assistant : RoleType.User,
                Content = [new TextContent { Text = m.Content }]
            })
            .ToList();

        logger.LogInformation("Calling Anthropic API for user {UserId} with {MessageCount} history messages", userId, sdkMessages.Count);

        var response = await anthropicClient.Messages.GetClaudeMessageAsync(new MessageParameters
        {
            Model = AnthropicModels.Claude45Haiku,
            MaxTokens = 1024,
            System = [new SystemMessage(systemPrompt)],
            Messages = sdkMessages
        });

        var reply = response.Content[0].ToString() ?? string.Empty;

        await repo.SaveMessageAsync(userId, sessionId, "assistant", reply);

        logger.LogInformation("Assistant replied to user {UserId} ({ReplyLength} chars)", userId, reply.Length);

        return reply;
    }

    public async Task<List<Models.Message>> GetHistoryAsync(string userId)
    {
        return await repo.GetHistoryByUserIdAsync(userId);
    }
}
