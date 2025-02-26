namespace EasyServer.Log;

public interface ILoggerFactory
{
    public ILogger CreateLogger(string categoryName);
    public ILogger CreateLogger(Type type);
    public ILogger<T> CreateLogger<T>();
}