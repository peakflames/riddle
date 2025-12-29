using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// In-memory event logging service (scoped per-circuit in Blazor Server).
/// Events are stored for the duration of the browser session and cleared on refresh.
/// </summary>
public class AppEventService : IAppEventService
{
    private readonly List<AppEvent> _events = [];
    private readonly object _lock = new();

    /// <inheritdoc />
    public int MaxEvents { get; } = 100;

    /// <inheritdoc />
    public event Action<AppEvent>? OnEventAdded;

    /// <inheritdoc />
    public void AddEvent(AppEventType type, string category, string message, string? details = null, bool isError = false)
    {
        var evt = new AppEvent
        {
            Type = type,
            Category = category,
            Message = message,
            Details = details,
            IsError = isError
        };

        AddEventInternal(evt);
    }
    
    /// <inheritdoc />
    public void AddToolEvent(AppEventType type, string toolName, string message, string? toolArgs = null, string? details = null, bool isError = false)
    {
        var evt = new AppEvent
        {
            Type = type,
            Category = toolName,
            Message = message,
            Details = details,
            IsError = isError,
            ToolName = toolName,
            ToolArgs = toolArgs
        };

        AddEventInternal(evt);
    }
    
    private void AddEventInternal(AppEvent evt)
    {
        lock (_lock)
        {
            _events.Add(evt);

            // Trim to max events (circular buffer behavior)
            while (_events.Count > MaxEvents)
            {
                _events.RemoveAt(0);
            }
        }

        // Invoke outside lock to prevent potential deadlocks
        OnEventAdded?.Invoke(evt);
    }

    /// <inheritdoc />
    public IReadOnlyList<AppEvent> GetEvents()
    {
        lock (_lock)
        {
            // Return newest first
            return _events.AsEnumerable().Reverse().ToList().AsReadOnly();
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }
}
