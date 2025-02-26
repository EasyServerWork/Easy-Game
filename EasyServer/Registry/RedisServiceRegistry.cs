namespace EasyServer.Registry;

[Scheme("redis")]
public class RedisServiceRegistry : IServiceRegistry
{
    public void AddListener(IServiceListener? listener)
    {
        throw new NotImplementedException();
    }

    public bool RemoveListener(IServiceListener listener)
    {
        throw new NotImplementedException();
    }

    public void RemoveAllListener()
    {
        throw new NotImplementedException();
    }

    public Task RegisterAsync(string key, string value)
    {
        throw new NotImplementedException();
    }

    public Task RegisterFuncAsync(string key, Func<string> func)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(string key, string value)
    {
        throw new NotImplementedException();
    }

    public Task Start(params IServiceListener[]? listeners)
    {
        throw new NotImplementedException();
    }

    public Task Stop()
    {
        throw new NotImplementedException();
    }
}