using BonddyPlatform.Services.DTOs.IncidentDtos;
using BonddyPlatform.Services.DTOs.Common;

namespace BonddyPlatform.Services.Interfaces;

public interface IIncidentService
{
    /// <summary>Báo cáo sự cố</summary>
    Task<IncidentResponseDto> CreateAsync(Guid reportedByUserId, IncidentCreateRequestDto dto);

    /// <summary>Lấy danh sách sự cố theo bookingId</summary>
    Task<IEnumerable<IncidentResponseDto>> GetByBookingIdAsync(Guid requesterUserId, Guid bookingId);

    /// <summary>CS/Admin cập nhật trạng thái giải quyết</summary>
    Task<IncidentResponseDto?> ResolveAsync(Guid resolverUserId, Guid id, IncidentResolveRequestDto dto);

    /// <summary>CS/Admin list incidents with filters</summary>
    Task<PagedResultDto<IncidentResponseDto>> SearchForOpsAsync(Guid requesterUserId, IncidentSearchRequestDto request);
}
