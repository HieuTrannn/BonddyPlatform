namespace BonddyPlatform.Services.DTOs.UserDtos;

public class BulkCreateUserResultDto
{
    public int TotalRequested { get; set; }
    public int TotalSucceeded { get; set; }
    public int TotalFailed { get; set; }
    public List<UserResponseDto> Succeeded { get; set; } = [];
    public List<BulkCreateUserFailedItemDto> Failed { get; set; } = [];
}

public class BulkCreateUserFailedItemDto
{
    public string Email { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
