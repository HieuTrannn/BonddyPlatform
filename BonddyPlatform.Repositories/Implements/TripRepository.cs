using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;

namespace BonddyPlatform.Repositories.Implements;

public class TripRepository : GenericRepository<Trip>, ITripRepository
{
    public TripRepository(BonddyDbContext context) : base(context)
    {
    }
}

