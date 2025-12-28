using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service interface for application event logging (debug/diagnostic purposes)
/// </summary>
public interface IAppEventService
{
    /// <summary>
    /// Add a new event to the log
    /// </summary>
    void AddEvent(AppEventType type, string category, string message, string? details = null, bool isError = false);

    /// <summary>
    /// Get all events (newest first)
    /// </summary>
    IReadOnlyList<AppEvent> GetEvents();

    /// <summary>
    /// Clear all events
    /// </summary>
    void Clear();

    /// <summary>
    /// Event notification for real-time updates
    /// </summary>
    event Action<AppEvent>? OnEventAdded;

    /// <summary>
    /// Maximum events to retain (circular buffer)
    /// </summary>
    int MaxEvents { get; }
}
