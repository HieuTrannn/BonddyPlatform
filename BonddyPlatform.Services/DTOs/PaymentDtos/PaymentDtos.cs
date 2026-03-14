using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Services.DTOs.PaymentDtos;

public class PaymentCreateRequestDto
{
    public Guid BookingId { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public string? ExternalRef { get; set; }
    public string? Note { get; set; }
}

public class PaymentTransitionRequestDto
{
    public PaymentStatus Status { get; set; }
    public string? ExternalRef { get; set; }
    public string? Note { get; set; }
}

public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string MethodName => Method.ToString();
    public PaymentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? ExternalRef { get; set; }
    public DateTime? HeldAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
