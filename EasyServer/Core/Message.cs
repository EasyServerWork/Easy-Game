namespace EasyServer.Core;


internal sealed class Request
{
    public string? FuncName { get; set; }
    public object[]? Parameters { get; set; }
    public object? RequestProxy { get; set; }
}


/// <summary>
/// 服务器之前通讯的消息，描述方法调用的消息
/// </summary>
internal sealed class ServerMessage
{
    public enum MessageType : byte
    { 
        None,
        Request,
        Response, 
    }

    public ulong RpcId { get; set; }

    // public BaseSource Source { get; set; }

    public MessageType Type { get; set; }

    internal Request? RequestObj { get; set; }

    internal Response? ResponseObj { get; set; }

    public bool IsRequest() => RpcId > 0 ;
}