using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.DTOs.ReviewDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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

        if (booking.TravelerUserId != reviewerUserId)
            throw new UnauthorizedAccessException("Only the traveler of this booking can submit a review.");

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

        // Update Buddy average rating (Buddy.Rate)
        var avg = await _uow.Bookings
            .GetQueryable()
            .Where(b => b.BuddyId == booking.BuddyId)
            .Join(_uow.Reviews.GetQueryable(),
                b => b.Id,
                r => r.BookingId,
                (b, r) => r)
            .Where(r => r.IsPublic)
            .AverageAsync(r => (double)r.Rating);

        var buddy = await _uow.Buddies.GetByIdAsync(booking.BuddyId);
        if (buddy != null)
        {
            buddy.Rate = Math.Round((decimal)avg, 2);
            buddy.UpdatedAt = DateTime.UtcNow;
            _uow.Buddies.Update(buddy);
            await _uow.SaveChangesAsync();
        }

        var full = await _uow.Reviews.GetByBookingIdAsync(review.BookingId);
        return ToDto(full!);
    }

    public async Task<ReviewResponseDto?> GetByBookingIdAsync(Guid bookingId)
    {
        var review = await _uow.Reviews.GetByBookingIdAsync(bookingId);
        return review is null ? null : ToDto(review);
    }

    public async Task<PagedResultDto<ReviewResponseDto>> GetPublicByBuddyIdAsync(Guid buddyId, int page, int pageSize)
    {
        var (items, total) = await _uow.Reviews.GetPublicByBuddyIdAsync(buddyId, page, pageSize);
        return new PagedResultDto<ReviewResponseDto>
        {
            Items = items.Select(ToDto),
            TotalCount = total,
            Page = page <= 0 ? 1 : page,
            PageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 50)
        };
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
