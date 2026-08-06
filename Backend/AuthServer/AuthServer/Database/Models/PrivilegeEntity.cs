using System.ComponentModel.DataAnnotations;

namespace AuthServer.Database.Models
{
    public class PrivilegeEntity
    {
        [Key]
        [Required]
        public string? Name { get; set; }
    }
}
