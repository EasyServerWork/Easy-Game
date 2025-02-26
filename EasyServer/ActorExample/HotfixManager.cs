using System.Collections.Concurrent;
using System.Linq.Expressions;
using EasyServer.Core;

namespace EasyServer.ActorExample;

public class HotfixManager
{
    
    private readonly ConcurrentDictionary<string, Func<BaseActor, object>> m_proxys = new();

    public void RegisterInterfaceImpl(string typeName, Type type)
    {
        // 检查类型是否为结构体
        bool isValueType = type.IsValueType;

        var constructor = type.GetConstructor(new Type[] { typeof(BaseActor) });
        if (constructor != null)
        {
            var parameter = Expression.Parameter(typeof(BaseActor), "actorModel");
            var newExpression = Expression.New(constructor, parameter);

            // 如果是结构体，需要显式装箱
            if (isValueType)
            {
                var boxExpression = Expression.Convert(newExpression, typeof(object));
                var lambda = Expression.Lambda<Func<BaseActor, object>>(boxExpression, parameter);
                var activator = lambda.Compile();
                m_proxys.AddOrUpdate(typeName, activator, (k, v) => activator);
            }
            else
            {
                var lambda = Expression.Lambda<Func<BaseActor, object>>(newExpression, parameter);
                var activator = lambda.Compile();
                m_proxys.AddOrUpdate(typeName, activator, (k, v) => activator);
            }
        }
    }
    
    
    public T CreateInterfaceObject<T>(string typeName, BaseActor actor)
    {
        if (m_proxys.TryGetValue(typeName, out var activator))
        {
            var result = activator(actor);
            if (result is T typedResult)
            {
                return typedResult;
            }
            throw new InvalidCastException($"无法将对象转换为类型 '{typeof(T).Name}'。");
        }
        throw new KeyNotFoundException($"未找到类型 '{typeName}' 的注册信息。");
    }
    
    
}