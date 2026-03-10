using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IPaymentRepository : IGenericRepository<Payment>
{
    Task<Payment?> GetByBookingIdAsync(Guid bookingId);
}
