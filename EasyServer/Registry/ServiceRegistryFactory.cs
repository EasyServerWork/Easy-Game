using System.Reflection;
using EasyServer.Utility;

namespace EasyServer.Registry;

public class ServiceRegistryFactory
{
    public IServiceRegistry CreateRegistry(RegistryConfig config, Assembly? assembly = null)
    {
        // 我们可以通过switch来选择不同的实现
        // switch (_config.Scheme)
        // {
        //     case "etcd":
        //         return new EtcdServiceRegistry(_config);
        //     default:
        //         throw new NotImplementedException();
        // }
        
        // 这样做的好处是，通过不同配置进行不同的实现，实现简单，问题是如果增加新的实现需要修改ServiceRegistryManager，
        // 如果未提供程序集，则使用调用方的程序集
        
        
        // 检查配置是否正确
        CheckConfig(config);
        
        if (assembly == null)
        {
            assembly = Assembly.GetExecutingAssembly();
        }
        
        // 查找程序集中所有实现了 IServiceRegistry 接口的类型
        var registryTypes = assembly.GetTypes()
            .Where(type => typeof(IServiceRegistry).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

        // 筛选出类名或某个属性与 _config.Scheme 匹配的类型
        var matchingTypes = registryTypes
            .Where(type => type.Name.Equals(config.Scheme, StringComparison.OrdinalIgnoreCase) ||
                           HasSchemeAttribute(type, config.Scheme))
            .ToList();

        if (matchingTypes.Count == 0)
        {
            throw new NotImplementedException($"No implementation found for scheme: {config.Scheme}");
        }
        else if (matchingTypes.Count > 1)
        {
            throw new InvalidOperationException($"Multiple implementations found for scheme: {config.Scheme}");
        }

        var matchingType = matchingTypes[0];

        // 检查类型的构造函数是否接受 RegistryConfig 参数
        var constructor = matchingType.GetConstructor(new[] { typeof(RegistryConfig) });
        if (constructor == null)
        {
            throw new InvalidOperationException($"No constructor found for {matchingType.Name} that accepts RegistryConfig");
        }

        // 实例化并返回
        try
        {
            return (IServiceRegistry)constructor.Invoke(new object[] { config });
        }
        catch (Exception ex)
        {
            // 记录日志或处理异常
            Console.WriteLine($"Failed to instantiate {matchingType.Name}: {ex.Message}");
            throw;
        }
    }
    
    private bool HasSchemeAttribute(Type type, string scheme)
    {
        // 假设我们有一个自定义的 [Scheme] 特性来标记实现类
        var schemeAttribute = type.GetCustomAttribute<SchemeAttribute>();
        return schemeAttribute != null && schemeAttribute.Scheme.Equals(scheme, StringComparison.OrdinalIgnoreCase);
    }



    private void CheckConfig(RegistryConfig config)
    {
        if (config == null)
        {
            throw new Exception("RegistryConfig is null");
        }

        if (config.ConnectionString == null)
        {
            throw new Exception("RegistryConfig.ConnectionString is null");
        }

        if (config.Prefixs.IsNullOrEmpty())
        {
            throw new Exception("RegistryConfig.ListenPrefixs is null");
        }

        if (HasConflictingPrefixes(config))
        {
            throw new Exception("RegistryConfig.ListenPrefixs has conflicting prefixes");
        }
    }
    
    /// <summary>
    /// 前缀是否存在冲突, 返回true表示有冲突
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    private bool HasConflictingPrefixes(RegistryConfig config)
    {
        if (config.Prefixs.IsNullOrEmpty())
        {
            // 空不存在冲突
            return false;
        }
        for (int i = 0; i < config.Prefixs.Length; i++)
        {
            for (int j = 0; j < config.Prefixs.Length; j++)
            {
                if (i != j && (config.Prefixs[j].StartsWith(config.Prefixs[i]) || config.Prefixs[i].StartsWith(config.Prefixs[j])))
                {
                    return true;
                }
            }
        }

        return false;
    }
    
}

// 自定义特性 [Scheme]
[AttributeUsage(AttributeTargets.Class)]
public class SchemeAttribute : Attribute
{
    public string Scheme { get; }

    public SchemeAttribute(string scheme)
    {
        Scheme = scheme;
    }
}