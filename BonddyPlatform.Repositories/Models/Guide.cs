using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

public class Guide
{
    public Guid Id { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }

    /// <summary>true = buddy trong hệ thống, false = external guide</summary>
    public bool IsInternal { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}