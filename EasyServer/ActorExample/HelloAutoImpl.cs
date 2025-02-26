using EasyServer.Core;

namespace EasyServer.ActorExample;

public struct HelloAutoImpl : IHelloActor
{
    public HelloActorModel _actorModel;
    
    public HelloAutoImpl(BaseActor actorModel)
    {
        _actorModel = (HelloActorModel)actorModel;
    }
    
    // public async Task<string> SayHello(string greeting)
    // {
    //     return await _actorModel.SayHello(greeting);
    // }
    public async Task<string> SayHello(string greeting)
    {
        return await _actorModel.SayHello("gggg");
    }
}