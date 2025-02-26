namespace EasyServer.Core;


public enum KeyType
{
    Long,
    String
}

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
public class ActorDefineAttribute(Type model, Type handler, KeyType keyType) : Attribute
{
    public Type? Model { get; set; } = model;
    public Type? Handler { get; set; } = handler;
    public KeyType? KeyType { get; set;} = keyType;
}