using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace EasyServer.Core;

public class HotfixManager
{
    private readonly ConcurrentDictionary<string, Func<BaseActor, object>> _actorProxyTypes = new();
    
    private readonly ConcurrentDictionary<string, Type> _types = new();
    
    private AssemblyLoadContext _assemblyLoadContext;
    
    private MetadataReference[] _metadataReferences;
    
    private string[] _dllPaths;
    
    public HotfixManager(params string[] dllPaths)
    {
        _dllPaths = dllPaths;
        // TODO: 如果没有给任何路径则从环境变量里取
    }
    

    public MetadataReference[] MRefs
    {
        get
        {
            return _metadataReferences;
        }
    }

    public void UnLoad()
    {
        //  TODO:
    }

    public void Load()
    {
        if (_dllPaths.Length <= 0)
        {
            // 必须要有值，否则抛出异常。
            throw new ArgumentException("_dllPaths 参数不能为空。");
        }
        
        var (hotfixAssemblies, mRefs) = GetHotfixAssemblies();
        _metadataReferences = mRefs;

        foreach (var assembly in hotfixAssemblies)
            ReloadHotfixAssembly(assembly);
    }
    
    
    private (Assembly[], MetadataReference[]) GetHotfixAssemblies()
    {
        string[] hotfixDllPaths = _dllPaths;
        
        _assemblyLoadContext?.Unload();
        GC.Collect();
        _assemblyLoadContext = new AssemblyLoadContext("Hotfix", true);

        var hotfixAssemblies = new Assembly[hotfixDllPaths.Length];
        var mRefs = new MetadataReference[hotfixDllPaths.Length];

        for (int i = 0; i < hotfixDllPaths.Length; i++)
        {
            var dllbytes = File.ReadAllBytes(hotfixDllPaths[i]);
            var pdbbytes = File.ReadAllBytes(hotfixDllPaths[i].Replace(".dll", ".pdb"));
            using var dllstream = new MemoryStream(dllbytes);
            using var pdbstream = new MemoryStream(pdbbytes);
            Assembly assembly = _assemblyLoadContext.LoadFromStream(dllstream, pdbstream);
            hotfixAssemblies[i] = assembly;
            dllstream.Seek(0, SeekOrigin.Begin);
            var mRef = MetadataReference.CreateFromStream(dllstream);
            mRefs[i] = mRef;
        }

        return (hotfixAssemblies, mRefs);
    }


    private void ReloadHotfixAssembly(Assembly ass)
    {
        foreach (var type in ass.GetTypes())
        {
            _types.AddOrUpdate(type.FullName, type, (k, v) => type);
            RegisterActorProxy(type);
        }
    }
    
    public void RegisterActorProxy(Type proxyType)
    {
        object[] objects = proxyType.GetCustomAttributes(typeof(ActorProxyAttribute), false);
        if (objects.Length == 0)
        {
            return;
        }
        
        var proxyTypeName = proxyType.FullName;
        
        // 使用Expression动态生成构造函数委托
        var constructorInfo = proxyType.GetConstructor(new[] { typeof(BaseActor) });
        if (constructorInfo == null)
        {
            throw new InvalidOperationException($"Proxy type {proxyTypeName} does not have a constructor with BaseActor parameter.");
        }

        // 创建表达式树： (BaseActor actor) => new ProxyType(actor)
        var parameter = Expression.Parameter(typeof(BaseActor), "model");
        var newExpression = Expression.New(constructorInfo, parameter);
        
        // 如果类型是class则使用下面这句，不需要进行装箱操作
        // var lambda = Expression.Lambda<Func<BaseActor, object>>(newExpression, parameter);
        
        // 将结构体装箱为object
        var convertExpression = Expression.Convert(newExpression, typeof(object));
    
        var lambda = Expression.Lambda<Func<BaseActor, object>>(convertExpression, parameter);

        // 编译表达式树为委托
        var activator = lambda.Compile();
        
        _actorProxyTypes.AddOrUpdate(proxyTypeName, activator, (k, v) => activator);
    }
    
    
    
    
    public void ScanActorProxy(Assembly ass)
    {
        foreach (var type in ass.GetTypes())
        {
            _types.AddOrUpdate(type.FullName, type, (k, v) => type);
            
            object[] objects = type.GetCustomAttributes(typeof(ActorProxyAttribute), false);
            if (objects.Length == 0)
            {
                continue;
            }
            RegisterActorProxy(type);
        }
    }

    public Type GetType(string typeName)
    {
        return _types.TryGetValue(typeName, out var type) ? type : null;
    }
    
    
    /// <summary>
    /// 创建Actor代理
    /// </summary>
    /// <param name="proxyName"></param>
    /// <param name="model"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidCastException"></exception>
    /// <exception cref="KeyNotFoundException"></exception>
    public T CreateActorProxy<T>(string proxyName, BaseActor model)
    {
        if (_actorProxyTypes.TryGetValue(proxyName, out var activator))
        {
            var result = activator(model);
            if (result is T typedResult)
            {
                return typedResult;
            }
            throw new InvalidCastException($"无法将对象转换为类型 '{typeof(T).Name}'。");
        }
        throw new KeyNotFoundException($"未找到类型 '{proxyName}' 的注册信息。");
    }
}