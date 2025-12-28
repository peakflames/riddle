namespace Riddle.Web.Services;

/// <summary>
/// Service for LLM communication via OpenRouter using LLM Tornado SDK.
/// Handles DM input processing, streaming responses, and tool call coordination.
/// </summary>
public interface IRiddleLlmService
{
    /// <summary>
    /// Process DM input and stream response with tool handling
    /// </summary>
    /// <param name="campaignId">The campaign context</param>
    /// <param name="dmMessage">The DM's message</param>
    /// <param name="onStreamToken">Callback for each streamed token</param>
    /// <param name="ct">Cancellation token</param>
    Task ProcessDmInputAsync(
        Guid campaignId, 
        string dmMessage,
        Func<string, Task> onStreamToken,
        CancellationToken ct = default);
}
