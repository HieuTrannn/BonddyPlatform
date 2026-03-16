using BonddyPlatform.API.Hubs;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BonddyPlatform.API.Realtime;

public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRRealtimeNotifier(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyUserAsync(string userId, string eventName, object payload)
    {
        return _hub.Clients.User(userId).SendAsync(eventName, payload);
    }
}

