namespace EasyServer.Core;

/// <summary>
/// 调用接口
/// </summary>
public interface IInvokable
{ 
    /// <summary>
    /// 调用方法，实现接口的类的实例里包含有一个真实的Delegate,方法会触发Delegate的执行
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public ValueTask<Response> Invoke(object target);

    /// <summary>
    /// 对于远程过来的方法调用，如果方法存在参数，进行参数的反序列化，这个玮Update实际上在做同样的事，只是这个是针对远程调用的参数赋值
    /// </summary>
    /// <param name="args"></param>
    public void Deserialize(List<byte[]> args);

    /// <summary>
    /// 如果方法存在参数，这里进行方法参数的赋值
    /// </summary>
    /// <param name="args"></param>
    public void Update(params object[] args);
} 

/// <summary>
/// 调用代理，对于一个Actor的方法调用，都会生成一个InvokableProxy，每个Actor暴露的方法都会有一个对应的实现
/// 这个实现通过源代码生成器进行生成。
/// </summary>
public abstract class InvokableProxy : IInvokable
{
    public async ValueTask<Response> Invoke(object target)
    {
        await InvokeInner(target);

        return Response.Completed;
    }

    public abstract Task InvokeInner(object target);

    public virtual void Deserialize(List<byte[]> args) { }
        
    public virtual void Update(params object[] args) {}
}

/// <summary>
/// 有返回结果的调用代理
/// </summary>
/// <typeparam name="TResult"></typeparam>
public abstract class InvokableProxy<TResult> : IInvokable
{
        
    public async ValueTask<Response> Invoke(object target)
    {
        var result = await InvokeInner(target);
        return Response.FromResult(result);
    }

    public abstract Task<TResult> InvokeInner(object target);

    public virtual void Deserialize(List<byte[]> args) { }
        
    public virtual void Update(params object[] args) {}
}
