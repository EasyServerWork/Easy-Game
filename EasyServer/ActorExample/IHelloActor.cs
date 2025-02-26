using EasyServer.Core;

namespace EasyServer.ActorExample;



[ActorDefine(model: typeof(HelloActorModel), handler: typeof(HelloActorHandler), keyType: KeyType.Long)]
public interface IHelloActor
{
    Task<string> SayHello(string greeting);
}