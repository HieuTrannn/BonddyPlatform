namespace BonddyPlatform.Repositories.Models;

/// <summary>
/// Maps a Role to a User — one user can hold multiple roles simultaneously.
/// e.g. A Buddy also keeps the User role.
/// </summary>
public class UserRole
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Role Role { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
