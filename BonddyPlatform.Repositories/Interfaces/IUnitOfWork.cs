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
        IUserRoleRepository UserRoles { get; }

        IOtpVerificationRepository OtpVerifications { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        ITripRepository Trips { get; }
        IChatRoomRepository ChatRooms { get; }
        IChatParticipantRepository ChatParticipants { get; }
        IMessageRepository Messages { get; }
        IBuddyRepository Buddies { get; }
        IBookingRepository Bookings { get; }
        IPaymentRepository Payments { get; }
        IReviewRepository Reviews { get; }
        IIncidentRepository Incidents { get; }
        Task<int> SaveChangesAsync();
    }
}
