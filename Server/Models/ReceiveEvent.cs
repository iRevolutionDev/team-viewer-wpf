namespace Server.Models;

public class ReceiveEvent
{
    public string EventName { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public string? Sender { get; set; } = string.Empty;
    public string? Target { get; set; } = string.Empty;
}