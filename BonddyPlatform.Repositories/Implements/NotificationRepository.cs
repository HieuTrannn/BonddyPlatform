using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;

namespace BonddyPlatform.Repositories.Implements;

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(BonddyDbContext context) : base(context)
    {
    }
}

