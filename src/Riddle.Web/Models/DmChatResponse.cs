namespace Riddle.Web.Models;

/// <summary>
/// Response from the DM chat LLM service.
/// Contains the assistant's response content and usage statistics.
/// </summary>
public record DmChatResponse(
    /// <summary>The assistant's response content (may contain Markdown)</summary>
    string Content,
    
    /// <summary>Whether the request completed successfully</summary>
    bool IsSuccess,
    
    /// <summary>Error message if IsSuccess is false</summary>
    string? ErrorMessage = null,
    
    /// <summary>Reasoning/thinking content from models that support it (e.g., o1, DeepSeek)</summary>
    string? Reasoning = null,
    
    /// <summary>Number of tokens in the prompt</summary>
    int? PromptTokens = null,
    
    /// <summary>Number of tokens in the completion</summary>
    int? CompletionTokens = null,
    
    /// <summary>Total tokens used (prompt + completion)</summary>
    int? TotalTokens = null,
    
    /// <summary>Number of tool calls made during this request</summary>
    int ToolCallCount = 0,
    
    /// <summary>Elapsed time in milliseconds</summary>
    long? DurationMs = null
);
