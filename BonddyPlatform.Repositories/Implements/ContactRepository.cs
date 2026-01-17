using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements
{
    public class ContactRepository : GenericRepository<Contact>, IContactRepository
    {
        public ContactRepository(BonddyDbContext context) : base(context) { }

        public async Task<Contact?> GetByGmailAsync(string gmail)
            => await _context.Contacts.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Gmail == gmail);

        public async Task<Contact?> GetByPhoneAsync(string phoneNumber)
            => await _context.Contacts.AsNoTracking()
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
    }
}
