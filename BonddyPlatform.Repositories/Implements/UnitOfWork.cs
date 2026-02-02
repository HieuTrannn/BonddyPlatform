using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Persistences;

namespace BonddyPlatform.Repositories.Implements
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BonddyDbContext _context;

        public IContactRepository Contacts { get; }
        public IUserRepository Users { get; }
        public IOtpVerificationRepository OtpVerifications { get; }
        public IRefreshTokenRepository RefreshTokens { get; }

        public UnitOfWork(
            BonddyDbContext context,
            IContactRepository contactRepository,
            IUserRepository userRepository,
            IOtpVerificationRepository otpVerificationRepository,
            IRefreshTokenRepository refreshTokenRepository)
        {
            _context = context;
            Contacts = contactRepository;
            Users = userRepository;
            OtpVerifications = otpVerificationRepository;
            RefreshTokens = refreshTokenRepository;
        }

        public Task<int> SaveChangesAsync()
            {
                return _context.SaveChangesAsync();
            }

        public void Dispose()
            {
                _context.Dispose();
            }
    }
}
