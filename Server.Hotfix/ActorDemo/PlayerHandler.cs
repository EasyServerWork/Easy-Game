using EasyServer.Core;
using Server.Model.ActorDemo;

namespace Server.Hotfix.ActorDemo;


[ActorDefine(model: typeof(PlayerModel), handler: typeof(PlayerHandler), keyType: KeyType.Long)]
public interface IPlayer
{
    Task Say(string str);
    
    Task<string> SayHello(string greeting);
}

public static class PlayerHandler
{
    public static async Task Say(this PlayerModel actor, string str)
    { 
        Console.WriteLine($"{actor.PlayerName} say: {str}");
    }
    
    public static Task<string> SayHello(this PlayerModel actor, string greeting)
    {
        Console.WriteLine($"{actor.PlayerName} say: {greeting}");

        return Task.FromResult("gggg");
    }
    
    public static async Task<string> SayHello2(string greeting)
    {
        return "HE";
    }
}