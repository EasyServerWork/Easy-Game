using EasyServer.Log;
using Microsoft.Extensions.Logging;
using ILogger = EasyServer.Log.ILogger;
using ILoggerFactory = EasyServer.Log.ILoggerFactory;
using LoggerFactory = EasyServer.Log.LoggerFactory;

namespace EasyServer.Core;

public static class LoggerManager
{
    private static ILoggerFactory _factory = new LoggerFactory(new LogConfig
    {
        Level = "debug"
    });

    public static void SetLoggerFactory(ILoggerFactory factory)
    {
        _factory = factory;
    }
    
    public static ILogger CreateLogger(Type type)
    {
        return _factory.CreateLogger(type);
    }
    
    public static ILogger CreateLogger(string categoryName)
    {
        return _factory.CreateLogger(categoryName);
    }
    public static Log.ILogger<T> CreateLogger<T>()
    {
        return _factory.CreateLogger<T>();
    }
}