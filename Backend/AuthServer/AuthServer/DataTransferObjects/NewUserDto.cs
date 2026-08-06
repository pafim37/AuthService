namespace AuthServer.DataTransferObjects
{
    public class NewUserDto
    {
        public string? Login { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
    }
}
