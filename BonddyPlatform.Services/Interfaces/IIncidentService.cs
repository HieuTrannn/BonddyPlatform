using BonddyPlatform.Services.DTOs.IncidentDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface IIncidentService
{
    /// <summary>Báo cáo sự cố</summary>
    Task<IncidentResponseDto> CreateAsync(Guid reportedByUserId, IncidentCreateRequestDto dto);

    /// <summary>Lấy danh sách sự cố theo bookingId</summary>
    Task<IEnumerable<IncidentResponseDto>> GetByBookingIdAsync(Guid bookingId);

    /// <summary>CS/Admin cập nhật trạng thái giải quyết</summary>
    Task<IncidentResponseDto?> ResolveAsync(Guid id, IncidentResolveRequestDto dto);
}
