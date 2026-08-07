using System.ComponentModel.DataAnnotations;

namespace AuthServer.Database.Models
{
    public class RefreshTokenEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string? TokenHash { get; set; }

        [Required]
        public DateTime ExpiresAtUtc { get; set; }

        public DateTime? RevokedAtUtc { get; set; }

        public Guid? ReplacedByTokenId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public UserEntity? User { get; set; }

        public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
    }
}
