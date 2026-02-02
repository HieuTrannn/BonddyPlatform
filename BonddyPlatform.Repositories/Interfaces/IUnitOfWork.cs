using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BonddyPlatform.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IContactRepository Contacts { get; }
        IUserRepository Users { get; }
        IOtpVerificationRepository OtpVerifications { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        Task<int> SaveChangesAsync();
    }
}
