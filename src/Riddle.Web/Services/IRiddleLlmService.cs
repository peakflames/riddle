using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for LLM communication via OpenRouter using LLM Tornado SDK.
/// Handles DM input processing, tool call coordination, and response generation.
/// </summary>
public interface IRiddleLlmService
{
    /// <summary>
    /// Process DM input and return full response with tool handling.
    /// Uses non-streaming API for reliable token usage tracking.
    /// </summary>
    /// <param name="campaignId">The campaign context</param>
    /// <param name="dmMessage">The DM's message</param>
    /// <param name="conversationHistory">Previous messages for context (excluding system prompt)</param>
    /// <param name="attachments">File attachments for the current message</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The assistant's response with usage statistics</returns>
    Task<DmChatResponse> ProcessDmInputAsync(
        Guid campaignId, 
        string dmMessage,
        IReadOnlyList<LlmConversationMessage>? conversationHistory = null,
        IReadOnlyList<LlmAttachment>? attachments = null,
        CancellationToken ct = default);
}
