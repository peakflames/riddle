namespace Riddle.Web.Models;

/// <summary>
/// Represents an application event for debug logging
/// </summary>
public record AppEvent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public AppEventType Type { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Details { get; init; }
    public bool IsError { get; init; }
    
    /// <summary>
    /// Tool name for ToolCall/ToolResult events
    /// </summary>
    public string? ToolName { get; init; }
    
    /// <summary>
    /// Tool arguments for ToolCall events (JSON or formatted string)
    /// </summary>
    public string? ToolArgs { get; init; }
}

/// <summary>
/// Categories of application events
/// </summary>
public enum AppEventType
{
    LlmRequest,      // DM input sent to LLM
    LlmResponse,     // LLM streaming response
    ToolCall,        // LLM requested a tool call
    ToolResult,      // Tool execution result
    StateUpdate,     // Game state was updated
    TokenUsage,      // Token usage statistics from LLM
    Error            // Error occurred
}
