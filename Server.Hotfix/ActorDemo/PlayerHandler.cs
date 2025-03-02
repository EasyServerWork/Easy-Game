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
        // return Task.FromResult("gggg");
        Console.WriteLine($"{actor.PlayerName} say: {str}");
    }
    
    public static async Task<string> SayHello(this PlayerModel actor, string greeting)
    {
        Console.WriteLine($"{actor.PlayerName} say: {greeting}");
    
    
        return "GGG";
        // return Task.FromResult("gggg");
    }
    
    public static async Task<string> SayHello2(string greeting)
    {
        return "HE";
    }
}



[ActorProxy]
public struct IPlayer_Proxy : IPlayer
{
    public PlayerModel _model;
    
    public IPlayer_Proxy(BaseActor model)
    {
        _model = (PlayerModel)model;
    }


    public async Task Say(string str)
    {
        await _model.Say(str);
        // throw new NotImplementedException();
    }

    public async Task<string> SayHello(string greeting)
    {
        return await _model.SayHello(greeting);
        // throw new NotImplementedException();
    }
}