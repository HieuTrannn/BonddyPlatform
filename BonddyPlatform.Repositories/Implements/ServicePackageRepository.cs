using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;

namespace BonddyPlatform.Repositories.Implements;

public class ServicePackageRepository : GenericRepository<ServicePackage>, IServicePackageRepository
{
    public ServicePackageRepository(BonddyDbContext context) : base(context)
    {
    }
}
