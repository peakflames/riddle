# Implementation Plan: Chat Attachments & Conversation History

[Overview]
Add file attachment support and conversation history to Riddle's DmChat component.

This plan addresses two critical gaps in Riddle's DmChat compared to Flowbite's AI Chat implementation:

1. **Conversation History (CRITICAL FIX)**: The LLM currently receives only the system prompt and current user message. Previous chat messages in the session are not sent, causing the LLM to lose context.

2. **File Attachments**: Add support for attaching images and text files to chat messages, matching Flowbite's implementation pattern.

The implementation preserves Riddle's existing system prompt and tool calling functionality while aligning with Flowbite's proven patterns for attachment handling and history management.

[Types]
Add attachment record type and update message model for conversation history.

### New Types in DmChat.razor @code block:

```csharp
/// <summary>
/// Represents a file attachment in the chat.
/// </summary>
private sealed record DmChatAttachment(
    string FileName,
    string ContentType,
    long Size,
    string Base64Data,
    bool IsImage,
    bool IsPlainText,
    string? TextContent = null,
    string? TextPreview = null);
```

### Update existing DmChatMessage record:
Add `IReadOnlyList<DmChatAttachment>? Attachments = null` parameter.

### New Service Layer Types (in Models folder or inline):

```csharp
/// <summary>
/// Attachment payload for LLM service.
/// </summary>
public sealed record LlmAttachment(
    string FileName,
    string ContentType,
    long Size,
    string Base64Data,
    bool IsImage,
    bool IsPlainText,
    string? TextContent = null);

/// <summary>
/// Message for conversation history.
/// </summary>
public sealed record LlmConversationMessage(
    string Role,  // "user" | "assistant" | "system"
    string Content,
    IReadOnlyList<LlmAttachment>? Attachments = null);
```

[Files]
Modify existing files to add attachment and history support.

### Files to Modify:

1. **src/Riddle.Web/Components/Chat/DmChat.razor**
   - Add PromptInputHeader, PromptInputAttachments, PromptInputActionMenu components
   - Add attachment state management and validation
   - Add helper methods for file processing
   - Update HandleSubmitAsync to include attachments
   - Update message history tracking

2. **src/Riddle.Web/Services/IRiddleLlmService.cs**
   - Update method signature to accept conversation history and attachments

3. **src/Riddle.Web/Services/RiddleLlmService.cs**
   - Update ProcessDmInputAsync to accept and use conversation history
   - Add attachment processing logic aligned with Flowbite's AiChatService

4. **src/Riddle.Web/Models/DmChatResponse.cs** (if exists, or add to existing)
   - Ensure response model supports any attachment-related metadata

### Files to Review (no changes expected):
- src/Riddle.Web/Services/ToolExecutor.cs (no changes)
- src/Riddle.Web/Services/IToolExecutor.cs (no changes)

[Functions]
Add new functions for attachment handling; modify existing for history support.

### DmChat.razor - New Functions:

```csharp
// Constants (match Flowbite)
private const long AttachmentSizeLimitBytes = 5 * 1024 * 1024;
private const int TextPreviewMaxCharacters = 160;
private static readonly string[] PlainTextExtensions = [".txt", ".md", ".markdown", ".log"];

// Attachment validation
private Task HandleAttachmentsChangedAsync(IReadOnlyList<PromptAttachment> attachments)

// Create attachment payloads for LLM
private async Task<IReadOnlyList<DmChatAttachment>> CreateAttachmentPayloadsAsync(IReadOnlyList<IBrowserFile> files)

// File processing helpers
private static async Task<byte[]> ReadFileBytesAsync(IBrowserFile file, CancellationToken ct = default)
private static bool IsPdfAttachment(string? contentType, string fileName)
private static bool IsPlainTextAttachment(string contentType, string fileName)
private static string? DecodeText(byte[] bytes)
private static string? CreateTextPreview(string? content)
private static string FormatFileSize(long bytes)
private static string? GetAttachmentPreviewSource(DmChatAttachment attachment)
```

### DmChat.razor - Modified Functions:

```csharp
// HandleSubmitAsync - Add:
// 1. Attachment processing before sending
// 2. Build conversation history from _messages list
// 3. Pass both to LlmService
```

### IRiddleLlmService - Modified Signature:

```csharp
Task<DmChatResponse> ProcessDmInputAsync(
    Guid campaignId,
    string dmMessage,
    IReadOnlyList<LlmConversationMessage>? conversationHistory = null,
    IReadOnlyList<LlmAttachment>? attachments = null,
    CancellationToken ct = default);
```

### RiddleLlmService - Modified Functions:

```csharp
// ProcessDmInputAsync - Add:
// 1. Accept conversation history parameter
// 2. Accept attachments parameter  
// 3. Build message list from history (like Flowbite's AiChatService)
// 4. Add attachments to current user message

// New helper:
private static ChatMessage CreateChatMessage(ChatMessageRoles role, string content, IReadOnlyList<LlmAttachment>? attachments)
```

[Classes]
No new classes required; modifications to existing service implementations.

### RiddleLlmService
- No structural changes to class
- Add private helper method for message creation with attachments
- Update ProcessDmInputAsync implementation

### DmChat (Blazor component, not a class)
- Add state fields for attachment tracking
- Add validation message field
- Add HasUnsupportedAttachments boolean

[Dependencies]
No new NuGet packages required.

The existing project already has:
- LlmTornado SDK (for ChatMessagePart with attachments)
- Flowbite.Blazor (for PromptInput* components)
- Microsoft.AspNetCore.Components.Forms (for IBrowserFile)

[Testing]
Manual testing steps for attachment and history features.

### Test Scenarios:

1. **Conversation History Test**
   - Send message "Hello, my name is Test"
   - Send follow-up "What is my name?"
   - LLM should correctly recall "Test" from history

2. **Image Attachment Test**
   - Drag & drop a JPG/PNG image
   - Verify preview appears in input area
   - Submit and verify image shows in message
   - Verify LLM can describe the image

3. **Text File Attachment Test**
   - Attach a .txt or .md file
   - Verify file info appears
   - Submit and verify text preview appears

4. **PDF Rejection Test**
   - Attempt to attach a PDF
   - Verify error message appears
   - Verify submit is disabled

5. **Size Limit Test**
   - Attempt to attach file > 5MB
   - Verify error handling

6. **Tool Calling with History Test**
   - Ask about game state
   - Verify tools still work correctly
   - Ask follow-up referencing previous response

[Implementation Order]
Sequential implementation steps to minimize conflicts.

1. **Add LLM types** - Create LlmAttachment and LlmConversationMessage records in Models folder
2. **Update IRiddleLlmService** - Add conversation history and attachments parameters
3. **Update RiddleLlmService** - Implement history and attachment handling in ProcessDmInputAsync
4. **Update DmChat state** - Add attachment state fields and constants
5. **Add DmChat helper methods** - Implement all attachment processing functions
6. **Update DmChat markup** - Add PromptInput attachment components
7. **Update HandleSubmitAsync** - Wire up history and attachments
8. **Test manually** - Run through all test scenarios
