namespace Riddle.Web.Services;

/// <summary>
/// Routes tool calls from LLM to appropriate handlers and returns results.
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Execute a tool by name with JSON arguments
    /// </summary>
    /// <param name="campaignId">The campaign context</param>
    /// <param name="toolName">Name of the tool to execute</param>
    /// <param name="argumentsJson">JSON-encoded arguments</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>JSON-encoded result</returns>
    Task<string> ExecuteAsync(
        Guid campaignId, 
        string toolName, 
        string argumentsJson, 
        CancellationToken ct = default);
}
