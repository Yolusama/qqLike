using System;

namespace qqLike.Model;

public class User
{
    public String Id { get; set; }
    public String Account { get; set; }
    public String Password { get; set; }
    public String Email { get; set; }
    public String Nickname { get; set; }
    public String Signature { get; set; }
    public DateTime Time { get; set; }
}