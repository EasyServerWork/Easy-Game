using EasyServer.Core;

namespace Server.Model.ActorDemo;

public class PlayerModel : BaseActor
{
    public long PlayerId { get; set; }
    public string PlayerName { get; set; }
}