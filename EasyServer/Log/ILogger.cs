using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace EasyServer.Log;

/// <summary>
/// 日志配置，游戏可以通过任何配置文件解析成 LogConfig
/// </summary>
public class LogConfig
{
    public required string Level { get; set; }
    
    // 文件路径，以 / 开头 或以 C: D:之类开头的都为绝对路径，在win下不能以 / 开头
    // 可以没有，没有则不进行文件输出
    public string? FileName { get; set; }
    
    // 是否输出到控制台, 默认输出
    public bool DisableConsole { get; set; }
    
    // 是否异步输出，默认异步
    public bool DisableAsync { get; set; }
    
    // 是否输出源代码行号，默认输出
    public bool DisableSourceLine { get; set; }
    
    // 是否禁止归档，默认不
    public bool DisableArchive { get; set; }

    // 开启归档下，最多保留多少个归档文件，默认30
    public int MaxArchive { get; set; } = 30;
        
    /// <summary>
    /// 过滤器，key 为日志记录器名称，value LogLevel
    /// </summary>
    public IDictionary<string, string>? Filters { get; set; }
}


/// <summary>
/// 日志记录器接口，在日志输出的方法里，没有采用param的方式，是为了能无性能消耗的获取到源码行
/// 参数上有 CallerMemberName, CallerFilePath, CallerLineNumber，标有这些特性的参数会在编译是给出数据，而不是运行时去反推调用栈
/// 从而节省开销
/// </summary>
public interface ILogger
{
    public void AddFixedProperties(params (string Key, string? Value)[] properties);

    public ILogger WithFixedProperties(params (string Key, string Value)[] properties);
    
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull;
    
    public bool IsEnabled(LogLevel logLevel);

    public void Debug(string? message, object?[] args,
        [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0);
    
    
    public void Debug(string? message, object? arg1 = null, object? arg2 = null,
        object? arg3 = null, object? arg4 = null, object? arg5 = null, object? arg6 = null, object? arg7 = null,
        object? arg8 = null, object? arg9 = null, object? arg10 = null, object? arg11 = null, object? arg12 = null,
        object? arg13 = null, object? arg14 = null, object? arg15 = null, object? arg16 = null,
        object? arg17 = null,
        [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0
    );

    public void Info(string? message, object?[] args,
        [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0);

    public void Info(string? message, object? arg1 = null, object? arg2 = null,
        object? arg3 = null, object? arg4 = null, object? arg5 = null, object? arg6 = null, object? arg7 = null,
        object? arg8 = null, object? arg9 = null, object? arg10 = null, object? arg11 = null, object? arg12 = null,
        object? arg13 = null, object? arg14 = null, object? arg15 = null, object? arg16 = null,
        object? arg17 = null,
        [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0
    );

    public void Warning(string? message, object?[] args,
        [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0);

    public void Warning(string? message, object? arg1 = null, object? arg2 = null,
        object? arg3 = null, object? arg4 = null, object? arg5 = null, object? arg6 = null, object? arg7 = null,
        object? arg8 = null, object? arg9 = null, object? arg10 = null, object? arg11 = null, object? arg12 = null,
        object? arg13 = null, object? arg14 = null, object? arg15 = null, object? arg16 = null,
        object? arg17 = null,
        [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0
    );
    
    public void Error(Exception? exception, string? message, object?[] args,
        [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0);

    public void Error(Exception? exception, [CallerLineNumber] int sourceLineNumber = 0,
        [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "");
    
    
    public void Error(Exception? exception, string? message, object? arg1 = null, object? arg2 = null,
        object? arg3 = null, object? arg4 = null, object? arg5 = null, object? arg6 = null, object? arg7 = null,
        object? arg8 = null, object? arg9 = null, object? arg10 = null, object? arg11 = null, object? arg12 = null,
        object? arg13 = null, object? arg14 = null, object? arg15 = null, object? arg16 = null,
        object? arg17 = null,
        [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0
    );
    
    
    public void Error(string? message, object?[] args,
        [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0);
    
    public void Error(string? message, object? arg1 = null, object? arg2 = null,
        object? arg3 = null, object? arg4 = null, object? arg5 = null, object? arg6 = null, object? arg7 = null,
        object? arg8 = null, object? arg9 = null, object? arg10 = null, object? arg11 = null, object? arg12 = null,
        object? arg13 = null, object? arg14 = null, object? arg15 = null, object? arg16 = null,
        object? arg17 = null,
        [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0);
}

public interface ILogger<out TCategoryName> : ILogger;

