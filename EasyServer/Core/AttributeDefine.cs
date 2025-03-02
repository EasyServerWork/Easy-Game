namespace EasyServer.Core;

public enum KeyType
{
    Long,
    String
}



/// <summary>
/// Actor定义，给接口定义Actor，用于关联(interface、model、handler)
/// </summary>
/// <param name="model"></param>
/// <param name="handler"></param>
/// <param name="keyType"></param>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
public class ActorDefineAttribute(Type model, Type handler, KeyType keyType) : Attribute
{
    public Type? Model { get; set; } = model;
    public Type? Handler { get; set; } = handler;
    public KeyType KeyType { get; set;} = keyType;
}

/// <summary>
/// Actor代理，代理一定是一个struct，并且肯定是自动生成的，对这类结构体打上ActorProxyAttribute，便用hotfix启动时扫描
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public class ActorProxyAttribute : Attribute
{}