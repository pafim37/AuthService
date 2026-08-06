using System.ComponentModel.DataAnnotations;

namespace AuthServer.Database.Models
{
    public class UserEntity
    {
        // TODO: Veyfy if string can be key here
        [Key]
        public string? Login { get; set; }
        public string? PasswordHashed { get; set; }
        public RoleEntity? Role { get; set; }
    }
}
