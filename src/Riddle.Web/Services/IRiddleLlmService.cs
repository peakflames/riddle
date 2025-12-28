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
    /// <param name="ct">Cancellation token</param>
    /// <returns>The assistant's response with usage statistics</returns>
    Task<DmChatResponse> ProcessDmInputAsync(
        Guid campaignId, 
        string dmMessage,
        CancellationToken ct = default);
}
