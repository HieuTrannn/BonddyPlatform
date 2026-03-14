using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

public class BookingSurcharge
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Type { get; set; } = string.Empty; // late_night, weekend, group_size, out_of_zone, custom

    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string? Note { get; set; }
}
