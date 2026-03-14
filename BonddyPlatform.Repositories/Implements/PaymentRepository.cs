using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(BonddyDbContext context) : base(context)
    {
    }

    public async Task<Payment?> GetByBookingIdAsync(Guid bookingId)
        => await _context.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.BookingId == bookingId);
}
