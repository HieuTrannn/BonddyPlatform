namespace BonddyPlatform.Services.Interfaces;

/// <summary>
/// Tracks which users currently have an active SignalR connection.
/// </summary>
public interface IPresenceTracker
{
    void UserConnected(Guid userId);
    void UserDisconnected(Guid userId);
    bool IsOnline(Guid userId);
}
