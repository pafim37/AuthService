using System.ComponentModel.DataAnnotations;

namespace AuthServer.Database.Models
{
    public class PrivilegeEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string? Name { get; set; }

        public string? Description { get; set; }
    }
}
