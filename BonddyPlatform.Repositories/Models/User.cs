using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

public class User
{
    public Guid Id { get; set; }
    [Required, MaxLength(255), EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required, MaxLength(500)]
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public string? PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? aboutMe { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>All roles assigned to this user.</summary>
    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
}

public enum Gender
{
    Male,
    Female,
    Other
}

public enum Role
{
    Admin,  // Platform administrator
    Buddy,  // Local buddy/guide
    CS,     // Customer Service
    User    // Regular traveler
}
