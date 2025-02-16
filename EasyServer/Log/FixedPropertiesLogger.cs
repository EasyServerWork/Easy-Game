using System.Runtime.CompilerServices;
using EasyServer.Utility;
using Microsoft.Extensions.Logging;

namespace EasyServer.Log;

internal struct LoggerMessage
{
    public string Message;
    public object?[] Args;
    public string MemberName;
    public string SourceFilePath;
    public int SourceLineNumber;
}

internal class FixedPropertiesLogger<T> : FixedPropertiesLogger, ILogger<T>
{
    public FixedPropertiesLogger(Microsoft.Extensions.Logging.ILogger logger, LoggerFactory factory) : base(logger, factory) { }
        
    public FixedPropertiesLogger(FixedPropertiesLogger fixedPropertiesLogger, params (string Key, string Value)[] fixedProperties) : base(fixedPropertiesLogger, fixedProperties) { }
}

internal class FixedPropertiesLogger : ILogger
{
    private Microsoft.Extensions.Logging.ILogger _logger;
    private IDictionary<string, string> _fixedProperties;
    private LoggerFactory _factory;
    
    public FixedPropertiesLogger(FixedPropertiesLogger fixedPropertiesLogger, params (string Key, string? Value)[] fixedProperties)
    {
        _logger = fixedPropertiesLogger._logger;
        _factory = fixedPropertiesLogger._factory;

        var newProperties = new Dictionary<string, string>(fixedPropertiesLogger._fixedProperties);
        foreach (var property in fixedProperties)
        {
            newProperties[property.Key] = property.Value;
        }
        this._fixedProperties = newProperties;
    }
    
    public FixedPropertiesLogger(Microsoft.Extensions.Logging.ILogger logger, LoggerFactory factory)
    {
        _logger = logger;
        _factory = factory;
            
        var newProperties = new Dictionary<string, string>();
        this._fixedProperties = newProperties;
    }

    public void AddFixedProperties(params (string Key, string? Value)[] properties)
    {
        var newProperties = new Dictionary<string, string>(this._fixedProperties);
        foreach (var property in properties)
        {
            newProperties[property.Key] = property.Value;
        }
        this._fixedProperties = newProperties;
    }

    public ILogger WithFixedProperties(params (string Key, string Value)[] properties)
    {
        return new FixedPropertiesLogger(this, properties);
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _logger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    private void Log(LogLevel logLevel, string? message, object?[] args, Exception? exception, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (_factory.Config.DisableSourceLine)
        {
            memberName = "";
            sourceFilePath = "";
            sourceLineNumber = 0;
        }

        var newMessage = new LoggerMessage()
        {
            Message = message,
            Args = args,
            MemberName = memberName,
            SourceFilePath = sourceFilePath,
            SourceLineNumber = sourceLineNumber
        };
        this.Log(logLevel, 0, newMessage, exception, null);
    }

    public void Debug(string? message, object?[] args, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        Log(LogLevel.Debug, message, args, null, memberName, sourceFilePath, sourceLineNumber);
    }

    public void Debug(string? message, object? arg1 = null, object? arg2 = null, object? arg3 = null, object? arg4 = null, object? arg5 = null, object? arg6 = null, object? arg7 = null, object? arg8 = null, object? arg9 = null, object? arg10 = null, object? arg11 = null, object? arg12 = null, object? arg13 = null, object? arg14 = null, object? arg15 = null, object? arg16 = null, object? arg17 = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        var args = new object?[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17 };
        Log(LogLevel.Debug, message, args, null, memberName, sourceFilePath, sourceLineNumber);
    }

    public void Info(string? message, object?[] args, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        Log(LogLevel.Information, message, args, null, memberName, sourceFilePath, sourceLineNumber);
    }

    public void Info(string? message, object? arg1 = null, object? arg2 = null, object? arg3 = null, object? arg4 = null, object? arg5 = null, object? arg6 = null, object? arg7 = null, object? arg8 = null, object? arg9 = null, object? arg10 = null, object? arg11 = null, object? arg12 = null, object? arg13 = null, object? arg14 = null, object? arg15 = null, object? arg16 = null, object? arg17 = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        var args = new object?[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17 };
        Log(LogLevel.Information, message, args, null, memberName, sourceFilePath, sourceLineNumber);
    }

    public void Warning(string? message, object?[] args, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        Log(LogLevel.Warning, message, args, null, memberName, sourceFilePath, sourceLineNumber);
    }

    public void Warning(string? message, object? arg1 = null, object? arg2 = null, object? arg3 = null, object? arg4 = null, object? arg5 = null, object? arg6 = null, object? arg7 = null, object? arg8 = null, object? arg9 = null, object? arg10 = null, object? arg11 = null, object? arg12 = null, object? arg13 = null, object? arg14 = null, object? arg15 = null, object? arg16 = null, object? arg17 = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        var args = new object?[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17 };
        Log(LogLevel.Warning, message, args, null, memberName, sourceFilePath, sourceLineNumber);
    }

    public void Error(Exception? exception, string? message, object?[] args, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        Log(LogLevel.Error, message, args, exception, memberName, sourceFilePath, sourceLineNumber);
    }

    public void Error(Exception? exception, [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "")
    {
        Log(LogLevel.Error, null, null, exception, memberName, sourceFilePath, sourceLineNumber);
    }

    public void Error(Exception? exception, string? message, object? arg1 = null, object? arg2 = null, object? arg3 = null, object? arg4 = null, object? arg5 = null, object? arg6 = null, object? arg7 = null, object? arg8 = null, object? arg9 = null, object? arg10 = null, object? arg11 = null, object? arg12 = null, object? arg13 = null, object? arg14 = null, object? arg15 = null, object? arg16 = null, object? arg17 = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        var args = new object?[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17 };
        Log(LogLevel.Error, message, args, exception, memberName, sourceFilePath, sourceLineNumber);
    }

    public void Error(string? message, object?[] args, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        Log(LogLevel.Error, message, args, null, memberName, sourceFilePath, sourceLineNumber);
    }

    public void Error(string? message, object? arg1 = null, object? arg2 = null, object? arg3 = null, object? arg4 = null, object? arg5 = null, object? arg6 = null, object? arg7 = null, object? arg8 = null, object? arg9 = null, object? arg10 = null, object? arg11 = null, object? arg12 = null, object? arg13 = null, object? arg14 = null, object? arg15 = null, object? arg16 = null, object? arg17 = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        var args = new object?[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17 };
        Log(LogLevel.Error, message, args, null, memberName, sourceFilePath, sourceLineNumber);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var properties = this._fixedProperties;
            
        if (state is LoggerMessage message)
        {
            string newMessage = message.Message;
            if (message.Message != null && message.Args != null && message.Args.Length > 0 && message.Args[0] != null)
            {
                newMessage = string.Format(message.Message, message.Args);
            }
            var fixedState = $"{newMessage} {exception} {message.SourceFilePath}:{message.SourceLineNumber} [{string.Join(", ", properties.Select(kv => $"{kv.Key}={kv.Value}"))}]";
            _logger.Log(logLevel, eventId, fixedState, exception, (s, e) => s);
            return;
        }
        else
        {
            var fixedState = $"[{string.Join(", ", properties.Select(kv => $"{kv.Key}={kv.Value}"))}] {formatter(state, exception)}";
            _logger.Log(logLevel, eventId, fixedState, exception, (s, e) => s);
        }
    }
}