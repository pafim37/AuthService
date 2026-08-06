namespace AuthServer.DataTransferObjects
{
    public class UserPatchDto
    {
        public string? Login { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
    }
}
