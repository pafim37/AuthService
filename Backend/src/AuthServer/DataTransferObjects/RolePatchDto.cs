namespace AuthServer.DataTransferObjects
{
    public class RolePatchDto
    {
        public string? Name { get; set; }
        public IEnumerable<string>? Privileges { get; set; }
    }
}
