using System.Collections.Concurrent;

namespace EasyServer.Core;



public class ActorSystem
{
    private ConcurrentDictionary<long, BaseActor> systems = new();
    
    public BaseActor Get(long actorId)
    {
        systems.TryGetValue(actorId, out var actor);
        return actor;
    }
    
    
}

public class ActorManager
{
    // private ConcurrentDictionary<uint, BaseService> _services = new();
    // private ConcurrentDictionary<string, uint> _name2Id = new();
    // private uint _curServiceId = 0;
    // private ILogger _logger;
    
    
    private ConcurrentDictionary<string, ActorSystem> systems = new();
    
    
    // 1. 通过接口获取到
    
    // public T Get<T>(long actorId)
    // {
    //     string name = typeof(T).FullName;
    //     var system = systems[name];
    //
    //     var actor = system.Get(actorId);
    //
    //     Type interfaceType = typeof(T);  // 接口类型
    //     Type actorModelType = actor.GetType();
    //     
    //     // 判断模型是否正确
    //     
    //     
    //     
    //     
    //     
    //     // _actors.TryGetValue()
    //     // return (T) _services[serviceId];
    //
    //     return null;
    // }
}