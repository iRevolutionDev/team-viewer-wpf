namespace Server.Models;

public class Chunk : ReceiveEvent
{
    public int ChunkSize { get; set; }
    public int Size { get; set; }
    public int FragmentId { get; set; }
    public int Sequence { get; set; }
}