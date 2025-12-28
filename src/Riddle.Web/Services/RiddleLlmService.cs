using System.Diagnostics;
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
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
            chat.AppendUserInput(dmMessage);

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
                    
                    // Emit individual tool execution event
                    _appEventService.AddEvent(
                        AppEventType.ToolCall, 
                        functionName, 
                        $"Executing tool: {functionName}",
                        functionArgs);
                    
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
                        
                        // Emit tool result event
                        _appEventService.AddEvent(
                            AppEventType.ToolResult, 
                            functionName, 
                            $"Tool completed: {functionName}",
                            result.Length > 500 ? result[..500] + "..." : result);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Tool {Tool} failed", functionName);
                        result = $"{{\"error\": \"{ex.Message}\"}}";
                        invocationSucceeded = false;
                        
                        // Emit error event
                        _appEventService.AddEvent(
                            AppEventType.Error, 
                            functionName, 
                            $"Tool failed: {functionName}",
                            ex.Message,
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
        var combatStatus = campaign.ActiveCombat?.IsActive == true 
            ? $"Yes (Round {campaign.ActiveCombat.RoundNumber})" 
            : "No";
        
        var partyList = campaign.PartyState.Count > 0
            ? string.Join(", ", campaign.PartyState.Select(c => $"{c.Name} ({c.Type}, HP: {c.CurrentHp}/{c.MaxHp})"))
            : "No characters registered";

        return $"""
            <<role_definition>>
            You are "Riddle," an expert Dungeon Master and Narrative Engine for D&D 5th Edition campaigns.
            You are currently running the "{campaign.CampaignModule}" campaign for "{campaign.Name}".
            <</role_definition>>

            <<system_constraints>>
            **Context Window & Memory:**
            - You are stateless. Every conversation may be a fresh start.
            - **MANDATORY STARTUP:** Your first tool call MUST be `get_game_state()` to understand the current reality.
            - **NO HALLUCINATION:** Never guess HP, conditions, or locations. Use only data from GameState.
            - **MANDATORY LOGGING:** Call `update_game_log()` after major events to preserve history.
            <</system_constraints>>

            <<interaction_model>>
            1. **The Software:** Holds UI, character sheets, dice rollers, and persistent state.
            2. **You (Riddle):** The "Brain." You calculate mechanics, generate narrative, and decide outcomes.
            3. **The Human DM:** Provides player actions and dice rolls. They do not calculate mechanics.
            <</interaction_model>>

            <<workflow_protocol>>
            For each DM input:
            1. **Recover:** Call `get_game_state()` if this is a new conversation.
            2. **Analyze Context:** Check `PartyPreferences` for tone/combat level, `ActiveQuests` for hooks.
            3. **Process:** Apply D&D 5e rules. Calculate DCs, attack rolls, damage internally.
            4. **Persist:** Call `update_game_log()` for events. Call `update_character_state()` for HP/condition changes.
            5. **Output:**
               - Use `display_read_aloud_text()` for atmospheric narration.
               - Use `present_player_choices()` for decision points.
               - Use `log_player_roll()` to show mechanical results.
               - For DM-only info (e.g., hidden enemy stats), reply in chat directly.
            <</workflow_protocol>>

            <<current_game_state>>
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
            <</current_game_state>>

            <<tone_and_style>>
            - Be a helpful mentor to the novice DM.
            - Explain the "why" behind mechanics briefly.
            - Be evocative and atmospheric in read-aloud text.
            - Adapt style based on PartyPreferences.
            <</tone_and_style>>
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
                "Sends atmospheric, boxed narrative text to the DM's Read Aloud Text Box.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        text = new { type = "string", description = "The prose to display in the Read Aloud Text Box" }
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
                "Records a dice roll result to the player dashboard.",
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
                }))
        ];
    }
}
