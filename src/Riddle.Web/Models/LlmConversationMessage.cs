namespace Riddle.Web.Models;

/// <summary>
/// Represents a message in the conversation history for LLM context.
/// Used to send previous chat messages to maintain context across turns.
/// </summary>
public sealed record LlmConversationMessage(
    /// <summary>Message role: "user", "assistant", or "system".</summary>
    string Role,
    
    /// <summary>Text content of the message.</summary>
    string Content,
    
    /// <summary>Optional attachments included with this message.</summary>
    IReadOnlyList<LlmAttachment>? Attachments = null);
