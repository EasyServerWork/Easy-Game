using Microsoft.Extensions.ObjectPool;

namespace EasyServer.Core;

internal readonly struct DefaultConcurrentObjectPoolPolicy<T> : IPooledObjectPolicy<T> where T : class, new()
{
    public T Create() => new();

    public bool Return(T obj) => true;
}