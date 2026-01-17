using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces
{
    public interface IContactRepository : IGenericRepository<Contact>
    {
        Task<Contact?> GetByGmailAsync(string gmail);
        Task<Contact?> GetByPhoneAsync(string phoneNumber);
    }
}
