using System.ComponentModel.DataAnnotations;

namespace AuthServer.Database.Models
{
    public class UserEntity
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public string? Login { get; set; }
        
        [Required]
        public string? PasswordHashed { get; set; }

        [Required]
        public Guid RoleId { get; set; }
        
        [Required]
        public RoleEntity? Role { get; set; }
    }
}
