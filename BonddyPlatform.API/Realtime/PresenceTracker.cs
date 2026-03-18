using System.Collections.Concurrent;
using BonddyPlatform.Services.Interfaces;

namespace BonddyPlatform.API.Realtime;

/// <summary>
/// In-memory singleton that tracks how many active SignalR connections each user has.
/// A user is considered online as long as they have at least one open connection.
/// </summary>
public class PresenceTracker : IPresenceTracker
{
    // userId -> number of active connections
    private readonly ConcurrentDictionary<Guid, int> _connections = new();

    public void UserConnected(Guid userId)
    {
        _connections.AddOrUpdate(userId, 1, (_, count) => count + 1);
    }

    public void UserDisconnected(Guid userId)
    {
        _connections.AddOrUpdate(userId, 0, (_, count) => Math.Max(0, count - 1));
        // Remove entry when no connections remain
        if (_connections.TryGetValue(userId, out var current) && current == 0)
            _connections.TryRemove(userId, out _);
    }

    public bool IsOnline(Guid userId)
    {
        return _connections.TryGetValue(userId, out var count) && count > 0;
    }
}
