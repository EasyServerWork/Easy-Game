using EasyServer.Core;
using Server.Model.ActorDemo;

namespace Server.Hotfix.ActorDemo;

public static class Example
{
    public static async Task Run()
    {
        PlayerModel player11 = new PlayerModel();
        
        ActorManager manager = new ActorManager();
        var player = manager.GetActor<IPlayer>(10025);


        var player1 = manager.GetActor<IPlayer>(10025);


        
        
        await player.Say("hello world");
        
        var str = await player.SayHello("hello world");
        
        Console.WriteLine("xxxx");
    }
}