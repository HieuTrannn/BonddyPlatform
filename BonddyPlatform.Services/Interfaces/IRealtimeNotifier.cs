namespace BonddyPlatform.Services.Interfaces;

public interface IRealtimeNotifier
{
    Task NotifyUserAsync(string userId, string eventName, object payload);
}

