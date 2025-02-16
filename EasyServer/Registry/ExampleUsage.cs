using EasyServer.Utility;

namespace EasyServer.Registry;

public class ExampleUsage
{
    public static async Task RunExample()
    {
        // 创建配置对象
        var config = RegistryConfig.CreateFromUri("etcd://127.0.0.1:2379?secure=false&prefixs=/u1,/u2");

        // 创建EtcdServiceRegistry实例
        var factory = new ServiceRegistryFactory();
        var registry = factory.CreateRegistry(config);

        // 添加监听器
        registry.AddListener(new ExampleListener());

        // 启动注册中心
        await registry.Start();

        Console.WriteLine("Hello, World!");

        // 注册一个服务
        await registry.RegisterAsync("/u1/my-service", "http://localhost:5000");

        await registry.RegisterAsync("/u2/my", "http://127");

        // 注册一个计算函数
        await registry.RegisterFuncAsync("/u1/health-check", () => Utils.GenerateUniqueId(16));

        long ss = 0;
        while (true)
        {
            ss++;
            try
            {
                await registry.UpdateAsync("/u1/my-service", "http://localhost:5000_" + ss);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            await Task.Delay(2000);
        }

        Console.WriteLine("Press any key to stop the registry...");
        Console.ReadKey();

        // 停止注册中心
        await registry.Stop();
    }
}


public class ExampleListener : IServiceListener
{
    public string[] Prefixes()
    {
        return new[] { "/u1", "/u2" };
    }

    public void OnEvent(EventData eventData)
    {
        Console.WriteLine($"Received event: {eventData.Type}");
        foreach (var content in eventData.Values)
        {
            Console.WriteLine($"Key: {content.Key}, Value: {content.Value}, Version: {content.Version}");
        }
    }
}