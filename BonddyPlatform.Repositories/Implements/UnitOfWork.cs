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
        public IUserRoleRepository UserRoles { get; }
        public IOtpVerificationRepository OtpVerifications { get; }
        public IRefreshTokenRepository RefreshTokens { get; }
        public ITripRepository Trips { get; }
        public IChatRoomRepository ChatRooms { get; }
        public IChatParticipantRepository ChatParticipants { get; }
        public IMessageRepository Messages { get; }
        public IBuddyRepository Buddies { get; }
        public IBookingRepository Bookings { get; }
        public IPaymentRepository Payments { get; }
        public IReviewRepository Reviews { get; }
        public IIncidentRepository Incidents { get; }

        public UnitOfWork(
            BonddyDbContext context,
            IContactRepository contactRepository,
            IUserRepository userRepository,
            IUserRoleRepository userRoleRepository,
            IOtpVerificationRepository otpVerificationRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ITripRepository tripRepository,
            IChatRoomRepository chatRoomRepository,
            IChatParticipantRepository chatParticipantRepository,
            IMessageRepository messageRepository,
            IBuddyRepository buddyRepository,
            IBookingRepository bookingRepository,
            IPaymentRepository paymentRepository,
            IReviewRepository reviewRepository,
            IIncidentRepository incidentRepository)
        {
            _context = context;
            Contacts = contactRepository;
            Users = userRepository;
            UserRoles = userRoleRepository;
            OtpVerifications = otpVerificationRepository;
            RefreshTokens = refreshTokenRepository;
            Trips = tripRepository;
            ChatRooms = chatRoomRepository;
            ChatParticipants = chatParticipantRepository;
            Messages = messageRepository;
            Buddies = buddyRepository;
            Bookings = bookingRepository;
            Payments = paymentRepository;
            Reviews = reviewRepository;
            Incidents = incidentRepository;
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
