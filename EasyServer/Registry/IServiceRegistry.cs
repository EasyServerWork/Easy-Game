using System.Text.Json;

namespace EasyServer.Registry;


public enum EventType
{
    Sync = 0,                // 收到事件需要全量替换
    Change = 1,              // 只变更
    Delete = 2               // 只删除
}

public class RegistryConfig
{
    /// <summary>
    /// 连接字符串
    /// </summary>
    public required string ConnectionString { get; set; }
    
    /// <summary>
    /// 是否使用安全连接
    /// </summary>
    public bool IsSecure { get; set; }
    
    /// <summary>
    /// 监听的key前缀
    /// </summary>
    public required string[] Prefixs { get; set; }
}


/// <summary>
/// 包装服务使用的值，值的内容这里不需要关心，由后续的监听器关心如何解析
/// </summary>
/// <param name="key"></param>
/// <param name="value"></param>
/// <param name="version"></param>
public class ServiceContent(string key, string value, long version = 0)
{
    public string Key { get; set; } = key;
    public string Value { get; set; } = value;
    public long Version { get; set; } = version;

    public override string ToString()
    {
        // 用json格式返回
        return JsonSerializer.Serialize(this);
    }
}


public class EventData(EventType type, params ServiceContent[] value)
{
    public EventType Type { get; set; } = type;
    public ServiceContent[] Values { get; set; } = value;
}


/// <summary>
/// 监听器接口，用于监听服务注册的事件，需要使用服务注册的模块进行实现，并添加到IServiceRegistry
/// </summary>
public interface IServiceListener
{
    /// <summary>
    /// 事件触发时的回调
    /// </summary>
    /// <param name="evt">事件数据</param>
    void OnEvent(EventData evt);
    
    /// <summary>
    /// 监听的key前缀,只有符合前缀的key才会触发OnEvent
    /// </summary>
    /// <returns></returns>
    string[] Prefixes();
}

/// <summary>
/// 服务注册中心，可以有不同的实现，默认采用etcd实现，整体设计思路是：
/// 这个服务注册只做事件分发，不关心注册的内容，只关心key和value，以及版本号，通过监听器分发事件
/// 监听器的实现，需要关心如何解析value，以及对数据的cache. 接口并未要求实现对数据cache，但默认的
/// etcd实现已经实现对数据的cache，cache的原始数据，这就意味着实现并不清楚数据的具体意义，目的只是为了
/// 事件发生时有原始数据做为判断的依据。
/// </summary>
public interface IServiceRegistry
{
    /// <summary>
    /// 添加监听器
    /// </summary>
    /// <param name="listener"></param>
    public void AddListener(IServiceListener listener);
    public bool RemoveListener(IServiceListener listener);
    public void RemoveAllListener();
    
    /// <summary>
    /// 注册一个服务 
    /// </summary>
    /// <param name="key">服务使用的key</param>
    /// <param name="value">注册的服务内容</param>
    /// <returns></returns>
    public Task RegisterAsync(string key, string value);
    
    /// <summary>
    /// 注册一个方法，这个方法会返回一个值，这个值会作为注册的内容，这个方法会每间隔一段时间重新调用变更注册内容
    /// 注册的内容会覆盖之前的内容，所以这个方法可以动态变更注册的内容。
    /// </summary>
    /// <param name="key"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public Task RegisterFuncAsync(string key, Func<string> func);
    
    /// <summary>
    /// 启动服务注册
    /// </summary>
    /// <returns></returns>
    public Task Start();
    
    public Task Stop();
}

// public ServiceRegistryManager