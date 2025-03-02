// See https://aka.ms/new-console-template for more information

using EasyServer.Core;

HotfixManager hotfix = new HotfixManager(@"C:\OpenSourceWork\Easy-Game\Server.Hotfix\bin\Debug\net9.0\Server.Hotfix.dll");

hotfix.Load();

GlobalContext.Hotfix = hotfix;

Type exampleType = hotfix.GetType("Server.Hotfix.ActorDemo.Example");

var method = exampleType.GetMethod("Run");
Task task = (Task)method.Invoke(null, null);

await task;

Console.WriteLine("Hello, World!");