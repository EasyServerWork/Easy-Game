using EasyServer.Log;
using Microsoft.Extensions.Logging;

namespace EasyServer.Log;

public class ExampleUsage
{
    public static void RunExample()
    {
        // 创建日志配置
        var logConfig = new LogConfig
        {
            Level = "Debug",
            FileName = @"c:\example.log",
            DisableConsole = false,
            DisableAsync = false,
            DisableSourceLine = false,
            DisableArchive = false,
            MaxArchive = 30,
            // Filters = new Dictionary<string, string>
            // {
            //     { "EasyServer", "Info" }
            // }
        };

        // 创建日志工厂
        var loggerFactory = new LoggerFactory(logConfig);

        // 创建日志记录器
        var logger = loggerFactory.CreateLogger<ExampleUsage>();

        // 添加固定属性
        logger.AddFixedProperties(("Application", "ExampleApp"), ("Version", "1.0.0"));

        // 记录不同级别的日志
        logger.Debug("This is a debug message.");
        logger.Info("This is an info message.");
        logger.Warning("This is a warning message.");
        logger.Error(new Exception("An error occurred."), "This is an error message with exception.");
    }
}