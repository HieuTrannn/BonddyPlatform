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

        public UnitOfWork(BonddyDbContext context, IContactRepository contactRepository)
        {
            _context = context;
            Contacts = contactRepository;
        }

        public Task<int> SaveChangesAsync()
            => _context.SaveChangesAsync();

        public void Dispose()
            => _context.Dispose();
    }
}
