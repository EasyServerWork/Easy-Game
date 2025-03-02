// See https://aka.ms/new-console-template for more information


using EasyServer.Core;


HotfixManager hotfix = new HotfixManager(@"C:\OpenSourceWork\Easy-Game\Server.Hotfix\bin\Debug\net9.0\Server.Hotfix.dll");

hotfix.Load();

GlobalContext.Hotfix = hotfix;

Type exampleType = hotfix.GetType("Server.Hotfix.ActorDemo.Example");

var method = exampleType.GetMethod("Run");
Task task = (Task)method.Invoke(null, null);

await task;

// ActorManager manager = new ActorManager();
// manager.GetActor<IPlayer>()

//C:\OpenSourceWork\Easy-Game\Server.Hotfix\bin\Debug\net9.0\Server.Hotfix.dll

// HotfixManager hotfix = new HotfixManager();

// hotfix.RegisterInterfaceImpl("IHelloActor", typeof(HelloAutoImpl));
//
// var actor = new HelloActorModel();
// var actorProxy = hotfix.CreateInterfaceObject<IHelloActor>("IHelloActor", actor);
//
//
// actorProxy.SayHello("ggggg");
//
//
//
// ExampleUsage.RunExample();

// var config = RegistryConfig.CreateFromUri("etcd://127.0.0.1:2379?secure=false&prefixs=/u1,/u2");
//
// var factory = new ServiceRegistryFactory();

// await ExampleUsage.RunExample();

// var registry = factory.CreateRegistry(config);
//
//
// await registry.Start(null);

Console.WriteLine("Press any key to stop the registry...");
Console.ReadKey();

Console.WriteLine("Hello, World!");

