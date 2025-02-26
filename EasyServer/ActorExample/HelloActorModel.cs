using EasyServer.Core;

namespace EasyServer.ActorExample;

public class HelloActorModel : BaseActor
{
    
}


public static class HelloActorHandler
{
    public static async Task<string> SayHello(this HelloActorModel actor, string greeting)
    {
        return "xxxx";
    }
}