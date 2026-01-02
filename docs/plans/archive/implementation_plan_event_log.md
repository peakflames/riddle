# Implementation Plan: Application Event Log Panel

[Overview]
Add a session-only debug log panel to the Campaign page that displays real-time application events including LLM tool calls, tool results, and service operations.

This feature addresses the visibility gap when debugging LLM interactions. Currently, when the LLM attempts a tool call but something fails (e.g., malformed XML or streaming issues), the raw output appears in the chat instead of being properly processed. The event log will provide transparency into the internal operations, making it easier to diagnose issues like the `<function_calls>` garbage appearing in the DM chat.

The implementation uses an in-memory scoped service pattern - events are captured during the browser session and cleared on page refresh. This keeps the implementation simple while providing immediate debugging value.

[Types]
Define event types and data structures for the logging system.

```csharp
// src/Riddle.Web/Models/AppEvent.cs
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
    Error            // Error occurred
}
```

[Files]
Files to create and modify for the event logging system.

**New Files:**
- `src/Riddle.Web/Models/AppEvent.cs` - Event type definitions (as shown above)
- `src/Riddle.Web/Services/IAppEventService.cs` - Service interface
- `src/Riddle.Web/Services/AppEventService.cs` - In-memory event service implementation
- `src/Riddle.Web/Components/Debug/AppEventLog.razor` - Event log display component

**Modified Files:**
- `src/Riddle.Web/Program.cs` - Register AppEventService as scoped service
- `src/Riddle.Web/Services/RiddleLlmService.cs` - Emit events for LLM operations
- `src/Riddle.Web/Services/ToolExecutor.cs` - Emit events for tool execution
- `src/Riddle.Web/Components/Pages/DM/Campaign.razor` - Add AppEventLog panel to right sidebar

[Functions]
Service methods and component logic to implement.

**IAppEventService Interface:**
- `void AddEvent(AppEventType type, string category, string message, string? details = null, bool isError = false)` - Add new event to log
- `IReadOnlyList<AppEvent> GetEvents()` - Get all events (newest first)
- `void Clear()` - Clear all events
- `event Action<AppEvent>? OnEventAdded` - Event notification for real-time updates
- `int MaxEvents { get; }` - Maximum events to retain (circular buffer)

**AppEventService Implementation:**
- Constructor with configurable `maxEvents` parameter (default: 100)
- Thread-safe `ConcurrentQueue<AppEvent>` or `List<AppEvent>` with lock
- Auto-trim to `MaxEvents` when adding new events
- Invoke `OnEventAdded` event when new event is added

**AppEventLog Component:**
- `[Parameter] public Guid? CampaignId { get; set; }` - Optional filter by campaign
- `[Parameter] public bool Expanded { get; set; } = false` - Collapsed by default
- `private List<AppEvent> _events` - Local cache of events
- `protected override void OnInitialized()` - Subscribe to `OnEventAdded`
- `public void Dispose()` - Unsubscribe from events
- `private void OnNewEvent(AppEvent evt)` - Handle new events, call `StateHasChanged`
- `private string GetEventIcon(AppEventType type)` - Return appropriate icon for event type
- `private string GetEventColor(AppEventType type, bool isError)` - Return Tailwind color classes

**RiddleLlmService Additions:**
- Inject `IAppEventService`
- Call `AddEvent(LlmRequest, ...)` when processing DM input
- Call `AddEvent(LlmResponse, ...)` when streaming tokens (batched)
- Call `AddEvent(ToolCall, ...)` when LLM requests tool
- Call `AddEvent(Error, ...)` on exceptions

**ToolExecutor Additions:**
- Inject `IAppEventService`
- Call `AddEvent(ToolCall, ...)` when executing tool
- Call `AddEvent(ToolResult, ...)` with result summary
- Call `AddEvent(Error, ...)` on tool failures

[Classes]
Class definitions and modifications.

**New Classes:**

1. `AppEvent` (record) - See [Types] section

2. `AppEventService : IAppEventService`
   - Private field: `List<AppEvent> _events`
   - Private field: `object _lock = new()`
   - Public property: `int MaxEvents { get; } = 100`
   - Public event: `Action<AppEvent>? OnEventAdded`
   - Methods: `AddEvent`, `GetEvents`, `Clear`

**Modified Classes:**

1. `RiddleLlmService`
   - Add constructor parameter: `IAppEventService appEventService`
   - Add private field: `readonly IAppEventService _appEventService`

2. `ToolExecutor`
   - Add constructor parameter: `IAppEventService appEventService`
   - Add private field: `readonly IAppEventService _appEventService`

[Dependencies]
No new NuGet packages required.

All functionality uses existing .NET and Flowbite Blazor capabilities:
- `System.Collections.Generic` for List/collections
- `System.Threading` for thread-safe operations
- Flowbite Blazor components (Card, Badge, Button, icons)

[Testing]
Manual verification approach.

**Verification Steps:**
1. Build project with `python build.py`
2. Run application with `python build.py run`
3. Navigate to an existing campaign
4. Verify event log panel appears collapsed in right sidebar
5. Expand event log and verify it shows events
6. Send a DM message and observe:
   - LlmRequest event appears
   - LlmResponse events appear (streaming)
   - ToolCall events appear when LLM invokes tools
   - ToolResult events show outcomes
7. Verify errors are highlighted in red
8. Verify "Clear" button works
9. Verify page refresh clears all events

[Implementation Order]
Sequential implementation steps to minimize conflicts.

1. **Create AppEvent model** (`src/Riddle.Web/Models/AppEvent.cs`)
   - Define `AppEvent` record
   - Define `AppEventType` enum

2. **Create AppEventService interface** (`src/Riddle.Web/Services/IAppEventService.cs`)
   - Define service contract

3. **Create AppEventService implementation** (`src/Riddle.Web/Services/AppEventService.cs`)
   - Implement in-memory event storage
   - Implement circular buffer logic

4. **Register service in Program.cs**
   - Add `AddScoped<IAppEventService, AppEventService>()`

5. **Create AppEventLog component** (`src/Riddle.Web/Components/Debug/AppEventLog.razor`)
   - Create collapsible card UI
   - Subscribe to service events
   - Display events with icons and colors

6. **Wire up RiddleLlmService**
   - Inject IAppEventService
   - Add event emissions at key points

7. **Wire up ToolExecutor**
   - Inject IAppEventService
   - Add event emissions at key points

8. **Add component to Campaign.razor**
   - Add AppEventLog to right sidebar below Read-Aloud Text card

9. **Build and verify**
   - Run `python build.py`
   - Manual testing per verification steps
