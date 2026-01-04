# Blazor Component Patterns

> **Keywords:** Parameter, EventCallback, AuthenticationStateProvider, StateHasChanged, combat state, InvokeAsync
> **Related:** [Flowbite Blazor](./flowbite-blazor.md), [SignalR Patterns](./signalr-patterns.md)

This document covers Blazor component lifecycle, parameter binding, and state management patterns.

---

## Authentication Patterns

- Get current user ID via `AuthenticationStateProvider.GetAuthenticationStateAsync()`
- User claims are accessed via `user.FindFirst(ClaimTypes.NameIdentifier)?.Value`
- Always check `user.Identity?.IsAuthenticated == true` before accessing claims
- Use `<AuthorizeView>` with `<Authorized>` and `<NotAuthorized>` sections for protected pages

---

## Combat State Management Pattern

**Combat state must be loaded from database on page initialization**, not just from SignalR events. The `CombatTracker` component receives state as a parameter from the parent page, so:

1. **On page load/refresh**: The parent page (e.g., `Campaign.razor`) must call `CombatService.GetCombatStateAsync()` to load persisted combat state
2. **During runtime**: SignalR events (`CombatStarted`, `TurnAdvanced`, `CombatEnded`) update the state in real-time
3. **Issue symptoms**: Stale combat data after refresh, or `end_combat` not clearing UI

```csharp
// ✅ CORRECT - Load combat state on page initialization
private async Task LoadCampaignAsync()
{
    campaign = await CampaignService.GetCampaignAsync(CampaignId);
    if (campaign != null)
    {
        _combatState = await CombatService.GetCombatStateAsync(CampaignId);  // Load from DB!
    }
}
```

---

## CRITICAL: [Parameter] Mutation Anti-Pattern

**Never directly modify `[Parameter]` properties in child components.** Parameters are owned by the parent component - modifying them locally creates a disconnected copy that doesn't trigger parent re-renders.

❌ **WRONG - Modifying parameter directly:**
```csharp
// Child component
[Parameter] public CombatStatePayload? Combat { get; set; }
[Parameter] public EventCallback<CombatStatePayload?> CombatChanged { get; set; }

_hubConnection.On(GameHubEvents.CombatEnded, async () =>
{
    Combat = null;  // WRONG! This creates a local copy, parent not notified properly
    await CombatChanged.InvokeAsync(null);
    await InvokeAsync(StateHasChanged);  // Re-renders with stale local state
});
```

✅ **CORRECT - Only invoke callback, let parent manage state:**
```csharp
_hubConnection.On(GameHubEvents.CombatEnded, async () =>
{
    // Don't modify Combat directly - it's a [Parameter] owned by parent
    // Just invoke the callback to notify parent to update its state
    await InvokeAsync(async () =>
    {
        await CombatChanged.InvokeAsync(null);
    });
});
```

**Key principle:** Child components should ONLY notify parents via `EventCallback` - never modify `[Parameter]` values directly. The parent sets the parameter value, which flows down to the child via normal Blazor parameter binding.

**Symptom:** UI doesn't update even though callbacks are invoked and SignalR events are received.

---

## Dynamic App Version Display

**Don't hardcode version strings in UI components.** Use reflection to read from `AssemblyInformationalVersionAttribute`:

```csharp
@using System.Reflection

private string AppInformationalVersion
{
    get
    {
        var appVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
        
        // Strip Git commit hash suffix (e.g., "0.9.0+abc123" → "0.9.0")
        var plusCharIndex = appVersion.IndexOf('+');
        if (plusCharIndex > -1)
        {
            appVersion = appVersion.Substring(0, plusCharIndex);
        }
        return appVersion;
    }
}
```

Then in markup: `<Badge>v@($"{AppInformationalVersion}")</Badge>`

Version is set in `.csproj` via `<Version>` or `<InformationalVersion>` property.
