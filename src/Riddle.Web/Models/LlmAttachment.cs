namespace Riddle.Web.Models;

/// <summary>
/// Represents a file attachment for LLM chat messages.
/// Contains processed file data ready for LLM consumption.
/// </summary>
public sealed record LlmAttachment(
    /// <summary>Original file name.</summary>
    string FileName,
    
    /// <summary>MIME content type (e.g., "image/png", "text/plain").</summary>
    string ContentType,
    
    /// <summary>File size in bytes.</summary>
    long Size,
    
    /// <summary>Base64-encoded file data.</summary>
    string Base64Data,
    
    /// <summary>True if this is an image attachment (content type starts with "image/").</summary>
    bool IsImage,
    
    /// <summary>True if this is a plain text file (.txt, .md, .log, etc.).</summary>
    bool IsPlainText,
    
    /// <summary>Decoded text content for plain text files.</summary>
    string? TextContent = null);
