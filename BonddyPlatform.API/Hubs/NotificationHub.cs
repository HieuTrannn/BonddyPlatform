using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BonddyPlatform.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    // Intentionally empty:
    // Server will push events (NotificationCreated) via IHubContext<NotificationHub>.
}

