using System.ComponentModel.DataAnnotations;

namespace AuthServer.Database.Models
{
    public class RoleEntity
    {
        [Key]
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public ICollection<PrivilegeEntity>? Privileges { get; set; }
    }
}
