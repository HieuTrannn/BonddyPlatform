using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models
{
    public class Contact
    {
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(255), EmailAddress]
        public string Gmail { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
