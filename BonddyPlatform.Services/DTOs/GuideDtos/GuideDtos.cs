namespace BonddyPlatform.Services.DTOs.GuideDtos;

public class GuideCreateRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsInternal { get; set; }
    public string? Notes { get; set; }
}

public class GuideUpdateRequestDto
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool? IsInternal { get; set; }
    public string? Notes { get; set; }
}

public class GuideResponseDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsInternal { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
