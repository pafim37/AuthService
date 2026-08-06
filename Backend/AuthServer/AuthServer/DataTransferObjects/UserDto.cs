namespace AuthServer.DataTransferObjects
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string? Login { get; set; }
        public RoleDto? Role { get; set; }
    }
}
