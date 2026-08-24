using System.Text.Json;

namespace AuthServer.DataTransferObjects
{
    public class PrivilegePatchDto
    {
        public string? Name { get; set; }
        public JsonElement? Description { get; set; }
    }
}
