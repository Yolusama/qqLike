using System;

namespace qqLike.Model;

public enum MessageType
{
    Common = 1,
    File
}

public class ChatMessage
{
    public int Port { get; set; }
    public String Nickname { get; set; }
    public String Type { get; set; }
    public Object Content { get; set; }
    public DateTime Time { get; set; }
}