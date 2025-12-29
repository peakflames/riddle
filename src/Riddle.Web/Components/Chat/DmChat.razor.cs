using System.Text;
using Flowbite.Components.Chat;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Riddle.Web.Models;
using Riddle.Web.Services;

namespace Riddle.Web.Components.Chat;

/// <summary>
/// DM-to-LLM chat interface using Flowbite Chat components.
/// Non-streaming implementation aligned with Flowbite Blazor AI Chat patterns.
/// </summary>
public partial class DmChat
{
    [Parameter]
    public Guid CampaignId { get; set; }

    [Inject]
    private IRiddleLlmService LlmService { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ILogger<DmChat> Logger { get; set; } = default!;

    // Message history
    private List<DmChatMessage> _messages = new();

    // Input state
    private string _inputText = string.Empty;
    private bool _isBusy;
    private string _busyLabel = "Riddle is thinking...";
    private PromptSubmissionStatus _submissionStatus = PromptSubmissionStatus.Idle;

    // Attachment state
    private string? _attachmentValidationMessage;
    private bool _hasUnsupportedAttachments;
    private bool _hasAttachments;
    private IReadOnlyList<IBrowserFile> _currentFiles = Array.Empty<IBrowserFile>();

    // Attachment constants (aligned with Flowbite ChatAiPage)
    private const long AttachmentSizeLimitBytes = 5 * 1024 * 1024; // 5MB
    private const int TextPreviewMaxCharacters = 160;
    private static readonly string[] PlainTextExtensions = [".txt", ".md", ".markdown", ".log"];

    private Task HandleTextChanged(string value)
    {
        _inputText = value;
        return Task.CompletedTask;
    }

    private Task HandleAttachmentsChangedAsync(IReadOnlyList<PromptAttachment> attachments)
    {
        _currentFiles = attachments?.Select(a => a.File).ToList() ?? [];
        _hasAttachments = _currentFiles.Count > 0;

        // Check for unsupported PDF attachments
        var hasPdf = attachments?.Any(att => IsPdfAttachment(att.ContentType, att.Name)) ?? false;
        _hasUnsupportedAttachments = hasPdf;
        _attachmentValidationMessage = hasPdf
            ? "PDF attachments are not supported. Remove the file to continue."
            : null;

        return InvokeAsync(StateHasChanged);
    }

    private async Task HandleSubmitAsync(PromptInputMessage prompt)
    {
        var userText = prompt.Text?.Trim() ?? string.Empty;

        // Block if unsupported attachments
        if (_hasUnsupportedAttachments)
        {
            _attachmentValidationMessage = "PDF attachments are not supported. Remove the file to continue.";
            await InvokeAsync(StateHasChanged);
            return;
        }

        // Process attachments
        IReadOnlyList<DmChatAttachment> attachments;
        try
        {
            attachments = await CreateAttachmentPayloadsAsync(prompt.Files);
        }
        catch (InvalidOperationException ex)
        {
            // Show error as assistant message
            _messages.Add(new DmChatMessage(
                Id: Guid.NewGuid(),
                Role: ChatMessageRole.Assistant,
                Content: $"**Attachment error:** {ex.Message}"));
            await InvokeAsync(StateHasChanged);
            return;
        }

        // Require text or attachments
        if (string.IsNullOrWhiteSpace(userText) && attachments.Count == 0)
            return;

        if (_isBusy)
            return;

        // Add user message with attachments
        var userMessage = new DmChatMessage(
            Id: Guid.NewGuid(),
            Role: ChatMessageRole.User,
            Content: string.IsNullOrWhiteSpace(userText) ? "Sent files for review." : userText,
            Attachments: attachments
        );
        _messages.Add(userMessage);

        // Set busy state and clear input
        _isBusy = true;
        _busyLabel = "Riddle is thinking...";
        _submissionStatus = PromptSubmissionStatus.Submitting;
        _inputText = string.Empty;
        _attachmentValidationMessage = null;
        _hasAttachments = false;
        _currentFiles = Array.Empty<IBrowserFile>();
        await InvokeAsync(StateHasChanged);

        try
        {
            // Build conversation history from previous messages (excluding current)
            var conversationHistory = _messages
                .Take(_messages.Count - 1) // Exclude the message we just added
                .Select(m => new LlmConversationMessage(
                    Role: m.Role == ChatMessageRole.User ? "user" : "assistant",
                    Content: m.Content,
                    Attachments: m.Attachments?.Select(a => new LlmAttachment(
                        FileName: a.FileName,
                        ContentType: a.ContentType,
                        Size: a.Size,
                        Base64Data: a.Base64Data,
                        IsImage: a.IsImage,
                        IsPlainText: a.IsPlainText,
                        TextContent: a.TextContent
                    )).ToList()
                ))
                .ToList();

            // Convert current attachments to LlmAttachment format
            var llmAttachments = attachments.Select(a => new LlmAttachment(
                FileName: a.FileName,
                ContentType: a.ContentType,
                Size: a.Size,
                Base64Data: a.Base64Data,
                IsImage: a.IsImage,
                IsPlainText: a.IsPlainText,
                TextContent: a.TextContent
            )).ToList();

            // Call LLM service with history and attachments
            var response = await LlmService.ProcessDmInputAsync(
                CampaignId,
                userText,
                conversationHistory.Count > 0 ? conversationHistory : null,
                llmAttachments.Count > 0 ? llmAttachments : null);

            if (response.IsSuccess)
            {
                var assistantMessage = new DmChatMessage(
                    Id: Guid.NewGuid(),
                    Role: ChatMessageRole.Assistant,
                    Content: response.Content,
                    Reasoning: response.Reasoning,
                    PromptTokens: response.PromptTokens,
                    CompletionTokens: response.CompletionTokens,
                    TotalTokens: response.TotalTokens,
                    ToolCallCount: response.ToolCallCount,
                    DurationMs: response.DurationMs
                );
                _messages.Add(assistantMessage);
            }
            else
            {
                // Error response
                var errorMessage = new DmChatMessage(
                    Id: Guid.NewGuid(),
                    Role: ChatMessageRole.Assistant,
                    Content: $"**Error:** {response.ErrorMessage}\n\nPlease check your API configuration and try again."
                );
                _messages.Add(errorMessage);
                _submissionStatus = PromptSubmissionStatus.Error;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing DM input");

            var errorMessage = new DmChatMessage(
                Id: Guid.NewGuid(),
                Role: ChatMessageRole.Assistant,
                Content: $"**Error:** {ex.Message}\n\nPlease check your API configuration and try again."
            );
            _messages.Add(errorMessage);
            _submissionStatus = PromptSubmissionStatus.Error;
        }
        finally
        {
            _isBusy = false;
            if (_submissionStatus != PromptSubmissionStatus.Error)
            {
                _submissionStatus = PromptSubmissionStatus.Idle;
            }
            await InvokeAsync(StateHasChanged);
        }
    }

    #region Attachment Helper Methods (ported from Flowbite ChatAiPage)

    private async Task<IReadOnlyList<DmChatAttachment>> CreateAttachmentPayloadsAsync(IReadOnlyList<IBrowserFile> files)
    {
        if (files is null || files.Count == 0)
        {
            return Array.Empty<DmChatAttachment>();
        }

        var payloads = new List<DmChatAttachment>(files.Count);

        foreach (var file in files)
        {
            if (file.Size > AttachmentSizeLimitBytes)
            {
                throw new InvalidOperationException(
                    $"\"{file.Name}\" ({FormatFileSize(file.Size)}) exceeds the {FormatFileSize(AttachmentSizeLimitBytes)} attachment limit.");
            }

            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;

            if (IsPdfAttachment(contentType, file.Name))
            {
                throw new InvalidOperationException("PDF attachments are not supported.");
            }

            var bytes = await ReadFileBytesAsync(file).ConfigureAwait(false);
            var base64Data = Convert.ToBase64String(bytes);
            var isPlainText = IsPlainTextAttachment(contentType, file.Name);
            string? textContent = null;
            string? preview = null;

            if (isPlainText)
            {
                textContent = DecodeText(bytes);
                if (!string.IsNullOrWhiteSpace(textContent))
                {
                    preview = CreateTextPreview(textContent);
                }
            }

            payloads.Add(new DmChatAttachment(
                FileName: file.Name,
                ContentType: contentType,
                Size: file.Size,
                Base64Data: base64Data,
                IsImage: contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase),
                IsPlainText: isPlainText,
                TextContent: textContent,
                TextPreview: preview));
        }

        return payloads;
    }

    private static async Task<byte[]> ReadFileBytesAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream(AttachmentSizeLimitBytes, cancellationToken);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private static bool IsPdfAttachment(string? contentType, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType) &&
            contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var extension = Path.GetExtension(fileName);
        return string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlainTextAttachment(string contentType, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType) &&
            contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrWhiteSpace(extension) &&
               PlainTextExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static string? DecodeText(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return null;
        }

        try
        {
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    private static string? CreateTextPreview(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var normalized = content.ReplaceLineEndings(" ").Trim();
        if (normalized.Length <= TextPreviewMaxCharacters)
        {
            return normalized;
        }

        return normalized[..TextPreviewMaxCharacters] + "…";
    }

    private static string FormatFileSize(long bytes)
    {
        var units = new[] { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024d;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{bytes} {units[unitIndex]}"
            : $"{size:0.##} {units[unitIndex]}";
    }

    private static string? GetAttachmentPreviewSource(DmChatAttachment attachment)
    {
        if (!attachment.IsImage)
        {
            return null;
        }

        var contentType = string.IsNullOrWhiteSpace(attachment.ContentType)
            ? "image/*"
            : attachment.ContentType;

        return $"data:{contentType};base64,{attachment.Base64Data}";
    }

    #endregion

    private async Task CopyMessageAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        try
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", content);
        }
        catch
        {
            // Clipboard may not be available
        }
    }

    /// <summary>
    /// Chat message with optional attachments.
    /// </summary>
    private sealed record DmChatMessage(
        Guid Id,
        ChatMessageRole Role,
        string Content,
        string? Reasoning = null,
        int? PromptTokens = null,
        int? CompletionTokens = null,
        int? TotalTokens = null,
        int ToolCallCount = 0,
        long? DurationMs = null,
        IReadOnlyList<DmChatAttachment>? Attachments = null);

    /// <summary>
    /// Attachment data for display in chat messages.
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
}
