using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace EasyServer.Log;

public class LoggerFactory : ILoggerFactory
{
    private LogConfig _config;

    public LogConfig Config { get => _config; }
    
    private Microsoft.Extensions.Logging.ILoggerFactory _loggerFactory;

    public LoggerFactory(LogConfig config)
    {
        _config = config;
        NLog.LogLevel logLevel = NLog.LogLevel.FromString(config.Level);
        
        // 配置参见 https://github.com/nlog/nlog/wiki/Configuration-API
        NLog.LogManager.Setup().LoadConfiguration(builder =>
        {
            // 设置过滤
            var setup = builder.ForLogger().FilterMinLevel(logLevel).FilterDynamicIgnore( logEventInfo =>
            {
                if (config.Filters == null || config.Filters.Count == 0)
                {
                    return false;
                }
                
                foreach (var (key, value) in config.Filters)
                {
                    if (logEventInfo.LoggerName.Contains(key))
                    {
                        return logEventInfo.Level < NLog.LogLevel.FromString(value);
                    }
                }
                return false;
            });
    
            string layout = "${longdate} ${level} ${all-event-properties:includeScopeProperties=true} ${message}";
    
            if (!config.DisableConsole)
            {
                // 设置打印控制台,(不同级别有默认的色彩)
                var consoleTarget = new ColoredConsoleTarget("console")
                {
                    Layout = layout,
                    Encoding = System.Text.Encoding.UTF8
                };
                setup.WriteTo(consoleTarget);
            }
            
            // 有文件写入
            if (!string.IsNullOrEmpty(config.FileName))
            {
                // 返回绝对路径的文件名
                var filename = FileName(config);
                
                // 组织文件目标
                var fileTarget = new FileTarget("file")
                {
                    FileName = filename,
                    Layout = layout
                };
                
                // 如果开启了归档
                if (!config.DisableArchive)
                {
                    fileTarget.ArchiveFileName = filename + "${shortdate}";
                    fileTarget.ArchiveEvery = FileArchivePeriod.Day;  // 每天归档一次
                    fileTarget.ArchiveNumbering = ArchiveNumberingMode.Date; // 按日期编号

                    var maxArchive = config.MaxArchive;
                    if (maxArchive <= 0)
                    {
                        maxArchive = 30;
                    }
                    fileTarget.MaxArchiveFiles = maxArchive; // 默认最大保留30个归档文件
                }
                
                // 禁止异步
                if (config.DisableAsync)
                {
                    setup.WriteTo(fileTarget);
                }
                else
                {
                    // 创建一个异步包装器
                    var asyncWrapper = new AsyncTargetWrapper(fileTarget)
                    {
                        QueueLimit = 5000,
                        OverflowAction = AsyncTargetWrapperOverflowAction.Grow
                    };
                    setup.WriteTo(asyncWrapper);
                }
            }
        });
        
        _loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddNLog(NLog.LogManager.Configuration);
        });
    }


    /// <summary>
    /// 获取文件名
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    private string FileName(LogConfig config)
    {
        // 如果配置中使用的是绝对路径就直接返回
        if (Path.IsPathRooted(config.FileName))
            return config.FileName;
        string currentDirectory = Directory.GetCurrentDirectory();
        return currentDirectory + Path.DirectorySeparatorChar + config.FileName;
    }
    
    
    public ILogger CreateLogger(string categoryName)
    {
        var baseLogger = _loggerFactory.CreateLogger(categoryName);
        var logger = new FixedPropertiesLogger(baseLogger, this);
        return logger;
    }
        
    public ILogger CreateLogger(Type type)
    {
        return CreateLogger(type.FullName);
    }
        
    public ILogger<T> CreateLogger<T>()
    {
        var baseLogger = _loggerFactory.CreateLogger<T>();
        var logger = new FixedPropertiesLogger<T>(baseLogger, this);
        return logger;
    }
}