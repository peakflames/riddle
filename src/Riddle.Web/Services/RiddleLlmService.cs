using System.Diagnostics;
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Images;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// LLM service using OpenRouter via LLM Tornado SDK.
/// Coordinates DM input processing, tool calling, and non-streaming responses.
/// </summary>
public class RiddleLlmService : IRiddleLlmService
{
    private readonly TornadoApi _api;
    private readonly IToolExecutor _toolExecutor;
    private readonly IGameStateService _stateService;
    private readonly IAppEventService _appEventService;
    private readonly ILogger<RiddleLlmService> _logger;
    private readonly string _defaultModel;
    
    /// <summary>
    /// Maximum tool call iterations to prevent infinite loops.
    /// </summary>
    private const int MaxToolIterations = 10;
    
    /// <summary>
    /// Maximum characters for plain text attachment content.
    /// </summary>
    private const int PlainTextAttachmentMaxChars = 20_000;

    public RiddleLlmService(
        IConfiguration config,
        IToolExecutor toolExecutor,
        IGameStateService stateService,
        IAppEventService appEventService,
        ILogger<RiddleLlmService> logger)
    {
        var apiKey = config["OpenRouter:ApiKey"] 
            ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
            ?? throw new InvalidOperationException("OpenRouter API key not configured. Set OpenRouter:ApiKey in appsettings.json or OPENROUTER_API_KEY environment variable.");
        
        _defaultModel = config["OpenRouter:DefaultModel"] ?? "deepseek/deepseek-chat";
        
        _api = new TornadoApi(new List<ProviderAuthentication>
        {
            new(LLmProviders.OpenRouter, apiKey)
        });
        
        _toolExecutor = toolExecutor;
        _stateService = stateService;
        _appEventService = appEventService;
        _logger = logger;
        
        _logger.LogInformation("RiddleLlmService initialized with model: {Model}", _defaultModel);
    }

    public async Task<DmChatResponse> ProcessDmInputAsync(
        Guid campaignId, 
        string dmMessage,
        IReadOnlyList<LlmConversationMessage>? conversationHistory = null,
        IReadOnlyList<LlmAttachment>? attachments = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var totalToolCalls = 0;
        
        try
        {
            var campaign = await _stateService.GetCampaignAsync(campaignId, ct);
            if (campaign == null)
            {
                return new DmChatResponse(
                    Content: string.Empty,
                    IsSuccess: false,
                    ErrorMessage: $"Campaign {campaignId} not found");
            }

            var systemPrompt = BuildSystemPrompt(campaign);
            var tools = BuildToolDefinitions();

            var chat = _api.Chat.CreateConversation(new ChatRequest
            {
                Model = _defaultModel,
                Tools = tools,
                ToolChoice = null, // Auto - let the model decide when to use tools
                Temperature = 0.7
            });

            chat.AppendSystemMessage(systemPrompt);
            
            // Add conversation history for context (critical for multi-turn conversations)
            if (conversationHistory is { Count: > 0 })
            {
                foreach (var msg in conversationHistory)
                {
                    if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                    {
                        // For user messages with attachments, use multipart format
                        if (msg.Attachments is { Count: > 0 })
                        {
                            var parts = BuildMessageParts(msg.Content, msg.Attachments);
                            chat.AppendUserInput(parts);
                        }
                        else
                        {
                            chat.AppendUserInput(msg.Content);
                        }
                    }
                    else
                    {
                        // Assistant messages are text only
                        chat.AppendMessage(ChatMessageRoles.Assistant, msg.Content);
                    }
                }
                
                _logger.LogInformation("Added {Count} history messages for context", conversationHistory.Count);
            }
            
            // Add current user message with any attachments
            if (attachments is { Count: > 0 })
            {
                var parts = BuildMessageParts(dmMessage, attachments);
                chat.AppendUserInput(parts);
            }
            else
            {
                chat.AppendUserInput(dmMessage);
            }

            _logger.LogInformation("Processing DM input for campaign {CampaignId}: {Message}", 
                campaignId, dmMessage.Length > 100 ? dmMessage[..100] + "..." : dmMessage);

            // Emit LLM request event
            _appEventService.AddEvent(
                AppEventType.LlmRequest, 
                "LLM", 
                $"Processing DM input ({dmMessage.Length} chars)",
                dmMessage.Length > 500 ? dmMessage[..500] + "..." : dmMessage);

            // Non-streaming: Get full response
            var response = await chat.GetResponseRich();
            
            // Handle tool calls in a loop (max iterations to prevent infinite loops)
            var iteration = 0;
            var functionBlocks = response?.Blocks?.Where(b => b.Type == ChatRichResponseBlockTypes.Function).ToList();
            
            while (functionBlocks != null && functionBlocks.Count > 0 && iteration < MaxToolIterations)
            {
                iteration++;
                
                _logger.LogInformation("LLM requested {Count} tool call(s) (iteration {Iteration})", 
                    functionBlocks.Count, iteration);
                
                // Emit tool call event
                _appEventService.AddEvent(
                    AppEventType.ToolCall, 
                    "LLM", 
                    $"LLM requested {functionBlocks.Count} tool call(s)",
                    string.Join("\n", functionBlocks.Select(c => $"• {c.FunctionCall?.Name ?? "unknown"}")));
                
                foreach (var block in functionBlocks)
                {
                    var functionName = block.FunctionCall?.Name ?? "unknown";
                    var functionArgs = block.FunctionCall?.Arguments ?? "{}";
                    var toolCallId = block.FunctionCall?.ToolCall?.Id ?? functionName;
                    
                    _logger.LogInformation("Executing tool: {Tool} (ID: {ToolCallId}) with args: {Args}", 
                        functionName, toolCallId, functionArgs.Length > 200 ? functionArgs[..200] + "..." : functionArgs);
                    
                    // Emit individual tool execution event with structured tool data
                    _appEventService.AddToolEvent(
                        AppEventType.ToolCall, 
                        functionName, 
                        $"Executing: {functionName}",
                        toolArgs: functionArgs);
                    
                    string result;
                    bool invocationSucceeded;
                    try
                    {
                        result = await _toolExecutor.ExecuteAsync(
                            campaignId, 
                            functionName, 
                            functionArgs,
                            ct);
                        
                        invocationSucceeded = true;
                        _logger.LogDebug("Tool {Tool} completed successfully", functionName);
                        
                        // Emit tool result event with structured tool data
                        _appEventService.AddToolEvent(
                            AppEventType.ToolResult, 
                            functionName, 
                            $"Completed: {functionName}",
                            details: result);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Tool {Tool} failed", functionName);
                        result = $"{{\"error\": \"{ex.Message}\"}}";
                        invocationSucceeded = false;
                        
                        // Emit error event with structured tool data
                        _appEventService.AddToolEvent(
                            AppEventType.Error, 
                            functionName, 
                            $"Failed: {functionName}",
                            toolArgs: functionArgs,
                            details: ex.Message,
                            isError: true);
                    }
                    
                    // Add tool result to conversation with proper ToolCallId for provider compatibility
                    chat.AddToolMessage(toolCallId, result, invocationSucceeded);
                    totalToolCalls++;
                }
                
                _logger.LogInformation("Continuing conversation after tool execution");
                _appEventService.AddEvent(
                    AppEventType.LlmResponse, 
                    "LLM", 
                    "Continuing after tool execution");
                
                // Get next response after tool results
                response = await chat.GetResponseRich();
                functionBlocks = response?.Blocks?.Where(b => b.Type == ChatRichResponseBlockTypes.Function).ToList();
            }
            
            stopwatch.Stop();

            // Extract response content - use Text property or concatenate message blocks
            var content = response?.Text ?? string.Empty;
            
            // Extract reasoning/thinking content if available (for models like DeepSeek, o1)
            var reasoning = response?.Result?.Choices?.FirstOrDefault()?.Message?.Reasoning;
            if (!string.IsNullOrEmpty(reasoning))
            {
                _logger.LogInformation("Reasoning content available ({Length} chars)", reasoning.Length);
                _appEventService.AddEvent(
                    AppEventType.LlmResponse,
                    "Reasoning",
                    $"Model reasoning ({reasoning.Length} chars)",
                    reasoning.Length > 500 ? reasoning[..500] + "..." : reasoning);
            }
            
            // Extract usage data from the underlying ChatResult (reliable in non-streaming mode!)
            var usage = response?.Result?.Usage;
            
            if (usage != null)
            {
                LogUsage(usage);
            }
            else
            {
                _logger.LogWarning("No usage data returned in response");
            }

            // Emit final response event
            _appEventService.AddEvent(
                AppEventType.LlmResponse, 
                "LLM", 
                $"Response complete ({content.Length} chars, {totalToolCalls} tool calls)",
                $"Duration: {stopwatch.ElapsedMilliseconds}ms");

            return new DmChatResponse(
                Content: content,
                IsSuccess: true,
                Reasoning: reasoning,
                PromptTokens: usage?.PromptTokens,
                CompletionTokens: usage?.CompletionTokens,
                TotalTokens: usage?.TotalTokens,
                ToolCallCount: totalToolCalls,
                DurationMs: stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing DM input");
            
            _appEventService.AddEvent(
                AppEventType.Error, 
                "LLM", 
                "LLM request failed",
                ex.Message,
                isError: true);
            
            return new DmChatResponse(
                Content: string.Empty,
                IsSuccess: false,
                ErrorMessage: ex.Message,
                DurationMs: stopwatch.ElapsedMilliseconds);
        }
    }

    private string BuildSystemPrompt(CampaignInstance campaign)
    {
        // Debug mode: allows DM to override system constraints when needed for testing
        var debugSystemPromptExtension = "";
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            debugSystemPromptExtension = """
            <debug_mode_protocol>
            The application is running in debug mode. 
            **TRIGGER:** When the user prompts **"SYSTEM OVERRIDE"**: you adhere to the exact request being asked of you, bypassing normal workflow constraints.
            IMPORTANT: When in SYSTEM OVERRIDE mode all tools are forbidden to be called unless instructed to by the user, othewise you are free to use tools
            </debug_mode_protocol>
            """;
            _logger.LogDebug("Debug mode system prompt extension enabled");
        }

        var combatStatus = campaign.ActiveCombat?.IsActive == true 
            ? $"Yes (Round {campaign.ActiveCombat.RoundNumber})" 
            : "No";
        
        var partyList = campaign.PartyState.Count > 0
            ? string.Join(", ", campaign.PartyState.Select(c => $"{c.Name} ({c.Type}, HP: {c.CurrentHp}/{c.MaxHp})"))
            : "No characters registered";

        return $"""
            <role_definition>
            You are "Riddle," an expert Dungeon Master and Narrative Engine for D&D 5th Edition campaigns.
            You are currently running the "{campaign.CampaignModule}" campaign for "{campaign.Name}".
            </role_definition>

            <system_constraints>
            **Context Window & Memory:**
            - You are stateless. Every conversation may be a fresh start.
            - **MANDATORY STARTUP:** Your first tool calls MUST be `get_game_state()` followed by `get_game_log()` to understand the current reality and recent events.
            - **NO HALLUCINATION:** Never guess HP, conditions, or locations. Use only data from GameState.
            - **MANDATORY LOGGING:** Call `update_game_log()` after major events to preserve history.
            </system_constraints>

            <interaction_model>
            1. **The Software:** Holds UI, character sheets, dice rollers, and persistent state.
            2. **You (Riddle):** The "Brain." You calculate mechanics, generate narrative, and decide outcomes.
            3. **The Human DM:** Provides player actions and dice rolls. They do not calculate mechanics.
            </interaction_model>

            <workflow_protocol>
            For each DM input:
            1. **Recover:** Call `get_game_state()` and get_game_log() if this is a new conversation.
            2. **Analyze Context:** Check `PartyPreferences` for tone/combat level, `ActiveQuests` for hooks.
            3. **Process:** Apply D&D 5e rules. Calculate DCs, attack rolls, damage internally.
            4. **Persist:** Call `update_game_log()` for events. Call `update_character_state()` for HP/condition changes.
            5. **Output:**
               - Use `display_read_aloud_text()` for atmospheric narration. Read Aloud Text (RAT). Generally this should be a short phrase. Emojis are supported but keep it subtle.
               - Use `present_player_choices()` for decision points and communicate options of what the player can do next.
               - Use `log_player_roll()` to show mechanical results.
               - For DM-only info (e.g., hidden enemy stats), reply in chat directly. Emojis where useful
            6. **Atmosphere Tools (Player Screens):**
               - Use `broadcast_atmosphere_pulse()` for transient sensory descriptions (sounds, smells, fleeting visuals) - auto-fades after ~10s
               - Use `set_narrative_anchor()` to establish persistent mood/context (danger nearby, safe haven found) - stays until changed
               - Use `trigger_group_insight()` for collective discoveries or revelations (party notices a clue)
               - These tools broadcast ONLY to Players - DM sees tool calls in the Event Log
            </workflow_protocol>

            <current_game_state>
            **Campaign:** {campaign.Name}
            **Module:** {campaign.CampaignModule}
            **Location:** {campaign.CurrentLocationId}
            **Chapter:** {campaign.CurrentChapterId}
            **Party:** {partyList}
            **Party Size:** {campaign.PartyState.Count} characters
            **Active Combat:** {combatStatus}
            **Last Summary:** {campaign.LastNarrativeSummary ?? "No previous summary available."}
            
            **Party Preferences:**
            - Combat Focus: {campaign.Preferences.CombatFocus}
            - Roleplay Focus: {campaign.Preferences.RoleplayFocus}
            - Pacing: {campaign.Preferences.Pacing}
            - Tone: {campaign.Preferences.Tone}
            </current_game_state>

            <tone_and_style>
            - Be a helpful mentor to the novice DM.
            - Explain the "why" behind mechanics briefly.
            - Be evocative and atmospheric in read-aloud text.
            - Adapt style based on PartyPreferences.
            - Avoid table formatting
            - Never reveal story secrets to character players
            </tone_and_style>

            <story_secrets>
            - AVOID revealing story secrets to charcter player in their choices
            - AVOID revealing story secrets to charcter player in the Read-Aloud Text.
            </story_secrets>

            <other_tips_and_tricks>
            - ULTRA IMPORTANT! IF this is a new conversation, IMMEDIATELY use the game data and game log to provide a short recap of what the campaign previous doing when we last playing the game
            </other_tips_and_tricks>

            <combat_protocol>
            **Combat Management Tools:**
            You have direct control over combat via these tools:

            1. `start_combat` - Initiates combat encounter
               - Provide enemy stats (name, initiative, max_hp, ac)
               - Provide PC initiative values (from DM-reported rolls)
               - This displays a visual Combat Tracker to ALL players
               - You don't need to describe turn order in chat - players SEE it

            2. `advance_turn` - Moves to next combatant's turn
               - Call this after resolving each combatant's action
               - Auto-increments round when returning to top of order

            3. `add_combatant` - Add enemy/ally mid-combat
               - For reinforcements, summoned creatures, etc.
               - Inserted at correct initiative order

            4. `remove_combatant` - Remove combatant from battle
               - For fled enemies, dismissed summons, etc.
               - Do NOT use for defeated enemies (track HP instead)

            5. `end_combat` - Ends the combat encounter
               - Call when all enemies defeated/fled OR party retreats
               - Clears Combat Tracker display

            **Combat Workflow:**
            1. When combat starts narratively → call `start_combat`
            2. Announce whose turn it is based on Combat Tracker state
            3. Player/DM describes action → you resolve mechanics
            4. Update HP via `update_character_state` if damage dealt
            5. Call `advance_turn` to move to next combatant
            6. Repeat until combat resolved
            7. Call `end_combat` to clear tracker

            **Important:**
            - All combat events are automatically logged to the narrative log
            - The Combat Tracker is VISUAL - players see HP bars, turn order, current turn
            - Don't repeat information that's already visible in the tracker
            </combat_protocol>

            {debugSystemPromptExtension}

            """;
    }

    private void LogUsage(ChatUsage usage)
    {
        _logger.LogInformation(
            "Token usage - Prompt: {Prompt}, Completion: {Completion}, Total: {Total}",
            usage.PromptTokens, usage.CompletionTokens, usage.TotalTokens);
        
        var details = $"Prompt: {usage.PromptTokens:N0} | Completion: {usage.CompletionTokens:N0} | Total: {usage.TotalTokens:N0}";
        
        // Add cache info if available
        if (usage.CacheReadTokens > 0 || usage.CacheCreationTokens > 0)
        {
            details += $"\nCache: {usage.CacheReadTokens:N0} read, {usage.CacheCreationTokens:N0} created";
        }
        
        // Add reasoning tokens if available (for models like o1)
        if (usage.CompletionTokensDetails?.ReasoningTokens > 0)
        {
            details += $"\nReasoning: {usage.CompletionTokensDetails.ReasoningTokens:N0} tokens";
        }
        
        _appEventService.AddEvent(
            AppEventType.TokenUsage,
            "Usage",
            $"Tokens: {usage.TotalTokens:N0}",
            details);
    }

    /// <summary>
    /// Builds message parts for multimodal content (text + attachments).
    /// </summary>
    private List<ChatMessagePart> BuildMessageParts(string text, IReadOnlyList<LlmAttachment> attachments)
    {
        var parts = new List<ChatMessagePart>();

        // Add text content if present
        if (!string.IsNullOrWhiteSpace(text))
        {
            parts.Add(new ChatMessagePart(text));
        }

        // Add attachments
        foreach (var attachment in attachments)
        {
            var contentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                ? "application/octet-stream"
                : attachment.ContentType;

            if (attachment.IsImage)
            {
                // Image attachments: use data URL format
                var dataUrl = $"data:{contentType};base64,{attachment.Base64Data}";
                parts.Add(new ChatMessagePart(dataUrl, ImageDetail.Auto, contentType));
                _logger.LogDebug("Added image attachment: {FileName} ({Size} bytes)", 
                    attachment.FileName, attachment.Size);
            }
            else if (attachment.IsPlainText && !string.IsNullOrWhiteSpace(attachment.TextContent))
            {
                // Plain text attachments: include content inline with truncation
                var truncated = attachment.TextContent.Length > PlainTextAttachmentMaxChars
                    ? attachment.TextContent[..PlainTextAttachmentMaxChars]
                    : attachment.TextContent;
                var labeledContent = $"Attachment ({attachment.FileName}):\n{truncated}";
                parts.Add(new ChatMessagePart(labeledContent));
                _logger.LogDebug("Added text attachment: {FileName} ({Length} chars)", 
                    attachment.FileName, truncated.Length);
            }
            else
            {
                // Other attachments: include as base64 document
                parts.Add(new ChatMessagePart(attachment.Base64Data, DocumentLinkTypes.Base64));
                _logger.LogDebug("Added binary attachment: {FileName}", attachment.FileName);
            }
        }

        return parts;
    }

    /// <summary>
    /// Creates a ChatMessage from an LlmConversationMessage, handling multipart content for attachments.
    /// </summary>
    private ChatMessage CreateChatMessage(LlmConversationMessage message)
    {
        var role = message.Role.ToLowerInvariant() switch
        {
            "user" => ChatMessageRoles.User,
            "assistant" => ChatMessageRoles.Assistant,
            "system" => ChatMessageRoles.System,
            _ => ChatMessageRoles.User
        };

        var parts = new List<ChatMessagePart>();

        // Add text content if present
        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            parts.Add(new ChatMessagePart(message.Content));
        }

        // Add attachments if present
        if (message.Attachments is { Count: > 0 })
        {
            foreach (var attachment in message.Attachments)
            {
                var contentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                    ? "application/octet-stream"
                    : attachment.ContentType;

                if (attachment.IsImage)
                {
                    // Image attachments: use data URL format
                    var dataUrl = $"data:{contentType};base64,{attachment.Base64Data}";
                    parts.Add(new ChatMessagePart(dataUrl, ImageDetail.Auto, contentType));
                    _logger.LogDebug("Added image attachment: {FileName} ({Size} bytes)", 
                        attachment.FileName, attachment.Size);
                    continue;
                }

                if (attachment.IsPlainText && !string.IsNullOrWhiteSpace(attachment.TextContent))
                {
                    // Plain text attachments: include content inline with truncation
                    var truncated = attachment.TextContent.Length > PlainTextAttachmentMaxChars
                        ? attachment.TextContent[..PlainTextAttachmentMaxChars]
                        : attachment.TextContent;
                    var labeledContent = $"Attachment ({attachment.FileName}):\n{truncated}";
                    parts.Add(new ChatMessagePart(labeledContent));
                    _logger.LogDebug("Added text attachment: {FileName} ({Length} chars)", 
                        attachment.FileName, truncated.Length);
                    continue;
                }

                // Other attachments: include as base64 document
                parts.Add(new ChatMessagePart(attachment.Base64Data, DocumentLinkTypes.Base64));
                _logger.LogDebug("Added binary attachment: {FileName}", attachment.FileName);
            }
        }

        // Return simple message if no parts, or multipart message if we have content
        if (parts.Count == 0)
        {
            return new ChatMessage(role, message.Content ?? string.Empty);
        }

        return new ChatMessage(role, parts);
    }

    private List<Tool> BuildToolDefinitions()
    {
        return
        [
            new Tool(new ToolFunction(
                "get_game_state",
                "Retrieves the full game state including character HP, locations, quests, and combat status. MUST be called first in any new conversation.")),
            
            new Tool(new ToolFunction(
                "update_character_state",
                "Updates a character's HP, conditions, initiative, or status notes.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        character_id = new { type = "string", description = "ID of the character to update" },
                        key = new 
                        { 
                            type = "string", 
                            @enum = new[] { "current_hp", "conditions", "status_notes", "initiative" },
                            description = "The property to update" 
                        },
                        value = new { description = "New value (int for HP/initiative, string[] for conditions, string for notes)" }
                    },
                    required = new[] { "character_id", "key", "value" }
                })),
            
            new Tool(new ToolFunction(
                "update_game_log",
                "Records an event to the narrative log for context recovery in future conversations.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        entry = new { type = "string", description = "Description of the event" },
                        importance = new 
                        { 
                            type = "string", 
                            @enum = new[] { "minor", "standard", "critical" },
                            description = "Importance level of this event" 
                        }
                    },
                    required = new[] { "entry" }
                })),
            
            new Tool(new ToolFunction(
                "display_read_aloud_text",
                "Sends atmospheric, boxed narrative text to the DM's Read Aloud Text Box with optional tone and pacing hints.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        text = new { type = "string", description = "The prose to display in the Read Aloud Text Box" },
                        tone = new { type = "string", description = "Optional tone hint (e.g., 'ominous', 'cheerful', 'tense', 'mysterious', 'epic')" },
                        pacing = new { type = "string", description = "Optional pacing hint (e.g., 'slow', 'normal', 'fast', 'building')" }
                    },
                    required = new[] { "text" }
                })),
            
            new Tool(new ToolFunction(
                "present_player_choices",
                "Sends interactive choice buttons to player screens.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        choices = new 
                        { 
                            type = "array", 
                            items = new { type = "string" },
                            description = "List of choices for players" 
                        }
                    },
                    required = new[] { "choices" }
                })),
            
            new Tool(new ToolFunction(
                "log_player_roll",
                "Records a dice roll result to the player dashboard and to the game log",
                new
                {
                    type = "object",
                    properties = new
                    {
                        character_id = new { type = "string", description = "Character who made the roll" },
                        check_type = new { type = "string", description = "Type of check (e.g., 'Perception')" },
                        result = new { type = "integer", description = "The dice roll result" },
                        outcome = new 
                        { 
                            type = "string", 
                            @enum = new[] { "Success", "Failure", "Critical Success", "Critical Failure" },
                            description = "Outcome of the roll"
                        }
                    },
                    required = new[] { "character_id", "check_type", "result", "outcome" }
                })),
            
            new Tool(new ToolFunction(
                "update_scene_image",
                "Updates the scene image displayed to players based on a description.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        description = new { type = "string", description = "Description for image generation or selection" }
                    },
                    required = new[] { "description" }
                })),
            
            new Tool(new ToolFunction(
                "get_game_log",
                "Retrieves recent narrative log entries for context recovery. Returns markdown-formatted history.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        limit = new { type = "integer", description = "Maximum entries to return (default 50, max 100)" }
                    }
                })),
            
            new Tool(new ToolFunction(
                "get_player_roll_log",
                "Retrieves recent dice roll results for all players. Returns markdown-formatted roll history.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        limit = new { type = "integer", description = "Maximum rolls to return (default 50, max 100)" }
                    }
                })),
            
            new Tool(new ToolFunction(
                "get_character_property_names",
                "Returns a categorized list of all queryable character property names with type hints.")),
            
            new Tool(new ToolFunction(
                "get_character_properties",
                "Retrieves specific properties for one or more characters. Returns markdown table.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        character_ids = new 
                        { 
                            type = "array", 
                            items = new { type = "string" },
                            description = "Character IDs to query" 
                        },
                        prop_names = new 
                        { 
                            type = "array", 
                            items = new { type = "string" },
                            description = "Property names to retrieve (use get_character_property_names for valid names)" 
                        }
                    },
                    required = new[] { "character_ids", "prop_names" }
                })),
            
            // Combat Management Tools
            new Tool(new ToolFunction(
                "get_combat_state",
                "Returns the current combat state including turn order, round number, HP status, and whose turn it is. Useful for recovering combat context or checking current state.")),
            
            new Tool(new ToolFunction(
                "start_combat",
                "Initiates a combat encounter with enemies and PC initiatives. Displays Combat Tracker to all players. Records entry into game log.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        enemies = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    name = new { type = "string", description = "Enemy name (e.g., 'Goblin 1', 'Orc Chief')" },
                                    initiative = new { type = "integer", description = "Initiative roll (1-30)" },
                                    max_hp = new { type = "integer", description = "Maximum hit points" },
                                    current_hp = new { type = "integer", description = "Current HP (defaults to max_hp if not provided)" },
                                    ac = new { type = "integer", description = "Armor Class (defaults to 10)" }
                                },
                                required = new[] { "name", "initiative", "max_hp" }
                            },
                            description = "List of enemy combatants"
                        },
                        pc_initiatives = new
                        {
                            type = "object",
                            description = "Map of character_id to initiative value for each PC",
                            additionalProperties = new { type = "integer" }
                        }
                    },
                    required = new[] { "enemies", "pc_initiatives" }
                })),
            
            new Tool(new ToolFunction(
                "end_combat",
                "Ends the current combat encounter and clears the Combat Tracker. . Records entry into game log.")),
            
            new Tool(new ToolFunction(
                "advance_turn",
                "Advances to the next combatant's turn. Auto-increments round when returning to top of order. Records entry into game log.")),
            
            new Tool(new ToolFunction(
                "add_combatant",
                "Adds a combatant to an active combat encounter (reinforcements, summons, etc.). Records entry into game log.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Combatant name" },
                        initiative = new { type = "integer", description = "Initiative roll (1-30)" },
                        max_hp = new { type = "integer", description = "Maximum hit points" },
                        current_hp = new { type = "integer", description = "Current HP (defaults to max_hp)" },
                        ac = new { type = "integer", description = "Armor Class (defaults to 10)" },
                        is_enemy = new { type = "boolean", description = "true=enemy, false=ally/summon" }
                    },
                    required = new[] { "name", "initiative", "max_hp", "is_enemy" }
                })),
            
            new Tool(new ToolFunction(
                "remove_combatant",
                "Removes a combatant from combat (fled, dismissed, etc.). Do NOT use for defeated enemies - track HP instead. Records entry into game log.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        combatant_name = new { type = "string", description = "Name or character_id of the combatant to remove" },
                        reason = new { type = "string", description = "Reason for removal (e.g., 'fled', 'dismissed', 'captured')" }
                    },
                    required = new[] { "combatant_name" }
                })),
            
            // Atmospheric Tools (Player Screens Only)
            new Tool(new ToolFunction(
                "broadcast_atmosphere_pulse",
                "Sends a fleeting, evocative sentence to the 'Atmosphere' section of all Player Screens. Use for transient mood and sensory details. Auto-fades after ~10 seconds.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        text = new { type = "string", description = "The atmospheric description to display (e.g., 'The torches flicker violently as a cold draft sweeps through.')." },
                        intensity = new { type = "string", @enum = new[] { "Low", "Medium", "High" }, description = "The urgency of the pulse to control animation speed or color." },
                        sensory_type = new { type = "string", @enum = new[] { "Sound", "Smell", "Visual", "Feeling" }, description = "The primary sense engaged to help the UI select an icon." }
                    },
                    required = new[] { "text" }
                })),
            
            new Tool(new ToolFunction(
                "set_narrative_anchor",
                "Updates the persistent 'Current Vibe' or 'Dungeon Instinct' banner at the top of the Player Screens. Use for persistent context that stays until explicitly changed.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        short_text = new { type = "string", description = "A concise fragment (max 10 words) summarizing the immediate feeling (e.g., 'The Ghost is still weeping nearby')." },
                        mood_category = new { type = "string", @enum = new[] { "Danger", "Mystery", "Safety", "Urgency" }, description = "The thematic mood to determine the UI border or color." }
                    },
                    required = new[] { "short_text" }
                })),
            
            new Tool(new ToolFunction(
                "trigger_group_insight",
                "Flashes a distinct notification on all Player Screens representing a collective observation or discovery. Auto-dismisses after 8-10 seconds.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        text = new { type = "string", description = "The specific clue or information discovered by the party." },
                        relevant_skill = new { type = "string", description = "The skill associated with the finding (e.g., 'Perception', 'History', 'Nature') for UI labeling." },
                        highlight_effect = new { type = "boolean", description = "If true, the text will shimmer or glow to indicate a critical clue." }
                    },
                    required = new[] { "text", "relevant_skill" }
                }))
        ];
    }
}
