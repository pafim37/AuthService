namespace AuthServer.DataTransferObjects
{
    public class RoleRequestDto
    {
        public string? Name { get; set; }
        public IEnumerable<string> Privileges { get; set; } = [];
    }
}
