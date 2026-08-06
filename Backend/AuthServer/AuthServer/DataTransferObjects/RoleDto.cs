namespace AuthServer.DataTransferObjects
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public IEnumerable<PrivilegeDto> Privileges { get; set; } = [];
    }
}
