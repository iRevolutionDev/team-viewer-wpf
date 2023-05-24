namespace Server.Models;

public class Client
{
    public string Ip { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public bool IsRequested { get; set; }
    public string ConnectedTo { get; set; } = string.Empty;
}