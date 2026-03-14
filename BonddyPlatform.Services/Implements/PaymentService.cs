using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.PaymentDtos;
using BonddyPlatform.Services.Interfaces;

namespace BonddyPlatform.Services.Implements;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;

    // Escrow state machine
    private static readonly Dictionary<PaymentStatus, HashSet<PaymentStatus>> _allowedTransitions = new()
    {
        [PaymentStatus.Pending]  = new() { PaymentStatus.Held, PaymentStatus.Refunded },
        [PaymentStatus.Held]     = new() { PaymentStatus.Released, PaymentStatus.Refunded },
        [PaymentStatus.Released] = new(),
        [PaymentStatus.Refunded] = new()
    };

    public PaymentService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PaymentResponseDto> CreateAsync(PaymentCreateRequestDto dto)
    {
        var existing = await _uow.Payments.GetByBookingIdAsync(dto.BookingId);
        if (existing is not null)
            throw new InvalidOperationException("A payment already exists for this booking.");

        var booking = await _uow.Bookings.GetByIdAsync(dto.BookingId)
            ?? throw new KeyNotFoundException($"Booking {dto.BookingId} not found.");

        var payment = new Payment
        {
            BookingId = dto.BookingId,
            Amount = booking.TotalAmount,
            Method = dto.Method,
            Status = PaymentStatus.Pending,
            ExternalRef = dto.ExternalRef,
            Note = dto.Note,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Payments.AddAsync(payment);
        await _uow.SaveChangesAsync();
        return ToDto(payment);
    }

    public async Task<PaymentResponseDto?> GetByBookingIdAsync(Guid bookingId)
    {
        var payment = await _uow.Payments.GetByBookingIdAsync(bookingId);
        return payment is null ? null : ToDto(payment);
    }

    public async Task<PaymentResponseDto?> GetByIdAsync(Guid id)
    {
        var payment = await _uow.Payments.GetByIdAsync(id);
        return payment is null ? null : ToDto(payment);
    }

    public async Task<PaymentResponseDto?> TransitionAsync(Guid id, PaymentTransitionRequestDto dto)
    {
        var payment = await _uow.Payments.GetByIdAsync(id);
        if (payment is null) return null;

        if (!_allowedTransitions.TryGetValue(payment.Status, out var allowed) || !allowed.Contains(dto.Status))
            throw new InvalidOperationException(
                $"Cannot transition payment from {payment.Status} to {dto.Status}.");

        payment.Status = dto.Status;
        payment.UpdatedAt = DateTime.UtcNow;

        if (dto.ExternalRef is not null) payment.ExternalRef = dto.ExternalRef;
        if (dto.Note is not null) payment.Note = dto.Note;

        switch (dto.Status)
        {
            case PaymentStatus.Held:
                payment.HeldAt = DateTime.UtcNow;
                break;
            case PaymentStatus.Released:
                payment.ReleasedAt = DateTime.UtcNow;
                break;
            case PaymentStatus.Refunded:
                payment.RefundedAt = DateTime.UtcNow;
                break;
        }

        _uow.Payments.Update(payment);
        await _uow.SaveChangesAsync();
        return ToDto(payment);
    }

    private static PaymentResponseDto ToDto(Payment p) => new()
    {
        Id = p.Id,
        BookingId = p.BookingId,
        Amount = p.Amount,
        Method = p.Method,
        Status = p.Status,
        ExternalRef = p.ExternalRef,
        HeldAt = p.HeldAt,
        ReleasedAt = p.ReleasedAt,
        RefundedAt = p.RefundedAt,
        Note = p.Note,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
