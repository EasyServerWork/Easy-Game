using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using EasyServer.Utility;
using NLog.Extensions.Logging;

namespace EasyServer.Core;



internal class ActorSystem
{
    private Type _modelType;       // 模型类型
    
    // 处理者类型的全名称（这里不使用Type，是因为handler会因为热加载而被替换，但名称是不会被替换的)
    private string _handlerFullName;
    
    // 接口全名称
    private string _interfaceFullName;
    
    private KeyType _keyType;
    
    private ConcurrentDictionary<CustomKey, BaseActor> _actors = new();
    
    internal ActorSystem(Type modelType, string handlerFullName, string interfaceFullName, KeyType keyType)
    {
        _modelType = modelType;
        _handlerFullName = handlerFullName;
        _interfaceFullName = interfaceFullName;
        _keyType = keyType;
    }
    

    internal BaseActor New(CustomKey key)
    {
        if (!_actors.TryGetValue(key, out var actor))
        {
            // 如果不存在，则创建新的Model实例
            actor = Activator.CreateInstance(_modelType) as BaseActor;
            
            // 将CustomKey设置到对象中
            actor.Key = key;
            // 将新实例添加到字典中
            _actors[key] = actor;
        }

        // 返回Model实例
        return actor;
    }

    /// <summary>
    /// 获取指定key的ActorProxy
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal T Get<T>(CustomKey key)
    {
        var model = New(key);
        string proxyTypeName = $"{_interfaceFullName}_Proxy";
        var proxy = GlobalContext.Hotfix.CreateActorProxy<T>(proxyTypeName, model);
        
        return proxy;
    }
}

public class ActorManager
{
    // 每一个接口对应一个system，不使用Type, 是因为interface会因为热加载被替换掉
    private ConcurrentDictionary<string, ActorSystem> _systems = new();
    
    internal ActorSystem GetSystem<T>()
    {
        // 检查T是否为接口类型
        if (!typeof(T).IsInterface)
        {
            throw new ArgumentException("Type T must be an interface.");
        }

        // 获取T的ActorDefine特性
        var actorDefineAttribute = typeof(T).GetCustomAttribute<ActorDefineAttribute>();
        if (actorDefineAttribute == null)
        {
            throw new ArgumentException($"Type T must have ActorDefine attribute.");
        }

        // 获取接口的全名称
        string interfaceFullName = typeof(T).FullName;
        
        // 检查_systems字典中是否已经存在对应的ActorSystem
        if (!_systems.TryGetValue(interfaceFullName, out var actorSystem))
        {
            string handlerFullName = actorDefineAttribute.Handler.FullName;
            Type modelType = actorDefineAttribute.Model;
            KeyType keyType = actorDefineAttribute.KeyType;

            if (!Utils.IsDerivedFromOrSame(modelType, typeof(BaseActor)))
            {
                throw new ArgumentException($"Type {modelType.FullName} must be derived from BaseActor.");
            }
            
            // 如果不存在，则创建新的ActorSystem并添加到字典中
            actorSystem = new ActorSystem(modelType, handlerFullName, interfaceFullName, keyType);
            _systems[interfaceFullName] = actorSystem;
        }

        return actorSystem;
    }
    
    public T GetActor<T>(CustomKey key)
    {
        var system = GetSystem<T>();
        var actor = system.Get<T>(key);
        return actor;
    }

    public T GetActor<T>(long actorId)
    {
        return GetActor<T>(new CustomKey(actorId));
    }
    
    public T GetActor<T>(string actorId)
    {
        return GetActor<T>(new CustomKey(actorId));
    }
}