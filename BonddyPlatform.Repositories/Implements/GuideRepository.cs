using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;

namespace BonddyPlatform.Repositories.Implements;

public class GuideRepository : GenericRepository<Guide>, IGuideRepository
{
    public GuideRepository(BonddyDbContext context) : base(context)
    {
    }
}
