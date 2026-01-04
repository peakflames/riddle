# SignalR Patterns

> **Keywords:** HubConnection, authorization, Docker URL, IServerSideBlazorBuilder, handler registration, groups
> **Related:** [Blazor Components](./blazor-components.md), [Testing Patterns](./testing-patterns.md)

This document covers SignalR connection patterns, authorization, and hub configuration gotchas.

---

## Hub Authorization

SignalR hub authorization uses **`[Authorize]` attribute on the hub class**, NOT route-based auth. The access token must be provided via the connection builder:

```csharp
_hubConnection = new HubConnectionBuilder()
    .WithUrl(Navigation.ToAbsoluteUri("/gameHub"), options =>
    {
        options.AccessTokenProvider = async () =>
        {
            // Get token from cookie or auth state
            return await GetAccessTokenAsync();
        };
    })
    .Build();
```

---

## Docker URL Configuration

**In Docker/production:** SignalR hub URLs must use **localhost or the container's internal hostname**, NOT the external `https://` URL. 

When the app runs in a container, components connect to the hub via the internal port:

```csharp
// ✅ CORRECT for containerized deployments
var hubUrl = Configuration["SignalR:InternalUrl"] ?? "/gameHub";

// ❌ WRONG - external URL fails inside container
var hubUrl = "https://riddle.example.com/gameHub";
```

Pattern: Use relative URLs (`/gameHub`) when possible - SignalR will use the current origin.

---

## CRITICAL: IServerSideBlazorBuilder Fluent API

When configuring Blazor Server, **`AddInteractiveServerComponents()` returns `IServerSideBlazorBuilder`** which must be captured for further configuration. The builder methods CANNOT be called after `Build()`.

```csharp
// ✅ CORRECT - Chain before Build()
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 1024 * 1024;  // 1MB
    });

// ❌ WRONG - Can't access builder after Build()
var app = builder.Build();
// app.Services.??? - Too late to configure hub options!
```

---

## Handler Registration Pattern

**Register SignalR handlers ONCE during component initialization**, not in lifecycle methods that may re-execute:

```csharp
protected override async Task OnInitializedAsync()
{
    // Register handlers ONCE
    _hubConnection.On<CombatStatePayload>(GameHubEvents.CombatStarted, async payload =>
    {
        await InvokeAsync(() =>
        {
            _combatState = payload;
            StateHasChanged();
        });
    });
    
    await _hubConnection.StartAsync();
}
```

**Always wrap handler callbacks in `InvokeAsync()`** to ensure thread-safe UI updates.

---

## Connection Lifecycle

- Start connection in `OnInitializedAsync`
- Dispose connection in `IAsyncDisposable.DisposeAsync()`
- Handle reconnection with `.WithAutomaticReconnect()`

```csharp
public async ValueTask DisposeAsync()
{
    if (_hubConnection is not null)
    {
        await _hubConnection.DisposeAsync();
    }
}
