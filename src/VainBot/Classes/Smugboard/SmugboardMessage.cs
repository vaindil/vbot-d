namespace VainBot.Classes.Smugboard;

public class SmugboardMessage
{
    public SmugboardMessage() { }
    
    public SmugboardMessage(ulong messageId)
    {
        MessageId = messageId;
    }
    
    public ulong MessageId { get; set; }
}