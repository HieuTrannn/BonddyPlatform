using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.ReviewDtos;
using BonddyPlatform.Services.Interfaces;

namespace BonddyPlatform.Services.Implements;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _uow;

    public ReviewService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ReviewResponseDto> CreateAsync(Guid reviewerUserId, ReviewCreateRequestDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var booking = await _uow.Bookings.GetByIdAsync(dto.BookingId)
            ?? throw new KeyNotFoundException($"Booking {dto.BookingId} not found.");

        if (booking.Status != BookingStatus.Completed)
            throw new InvalidOperationException("Reviews can only be submitted for completed bookings.");

        var existing = await _uow.Reviews.GetByBookingIdAsync(dto.BookingId);
        if (existing is not null)
            throw new InvalidOperationException("A review already exists for this booking.");

        var review = new Review
        {
            BookingId = dto.BookingId,
            ReviewerUserId = reviewerUserId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            IsPublic = dto.IsPublic,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Reviews.AddAsync(review);
        await _uow.SaveChangesAsync();

        var full = await _uow.Reviews.GetByBookingIdAsync(review.BookingId);
        return ToDto(full!);
    }

    public async Task<ReviewResponseDto?> GetByBookingIdAsync(Guid bookingId)
    {
        var review = await _uow.Reviews.GetByBookingIdAsync(bookingId);
        return review is null ? null : ToDto(review);
    }

// đâ
    private static ReviewResponseDto ToDto(Review r) => new()
    {
        Id = r.Id,
        BookingId = r.BookingId,
        ReviewerUserId = r.ReviewerUserId,
        ReviewerName = r.ReviewerUser?.FullName ?? string.Empty,
        Rating = r.Rating,
        Comment = r.Comment,
        IsPublic = r.IsPublic,
        CreatedAt = r.CreatedAt
    };
}
