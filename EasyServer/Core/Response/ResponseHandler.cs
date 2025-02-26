using System.Collections.Concurrent;
using EasyServer.Log;
using EasyServer.Utility;

namespace EasyServer.Core;

internal class CallBack<TMessage>
{
    internal TMessage Packet { get; set; }
    internal ulong RpcId { get; set; }

    private readonly IResponseCompletionSource _respSource;

    private Stopwatch _stopwatch;

    private int _completed;

    public bool IsCompleted => this._completed == 1;
    
    public Type? GetResponseType()
    {
        return _respSource.GetResultType();
    }

    public CallBack(
        IResponseCompletionSource respSource,
        TMessage message
    )
    {
        _stopwatch = new Stopwatch();
        _respSource = respSource;
        this.Packet = message;
    }

    public void Do(Response response)
    {
        if (Interlocked.CompareExchange(ref this._completed, 1, 0) != 0)
        {
            return;
        }

        // TODO 调用时间监控
        var duration = _stopwatch.Elapsed;
        ResponseCallback(response);
    }

    private void ResponseCallback(Response response)
    {
        try
        {
            _respSource.Complete(response);
        }
        catch (Exception ex)
        {
            _respSource.Complete(Response.FromException(ex));
        }
    }
}

internal class ResponseHandler
{
    private readonly ConcurrentDictionary<ulong, CallBack<ServerMessage>> _callbacks;
    private ILogger _logger { get; set; }

    public ResponseHandler(ILogger logger)
    {
        _callbacks = new();
        _logger = logger;
    }
    
    
    internal void ReceiveResponse(ServerMessage message)
    {
        CallBack<ServerMessage> callback;
        bool found = _callbacks.TryRemove(message.RpcId, out callback);
        if (found)
        {
            callback.Do(message.ResponseObj);
        }
        else
        {
            _logger.Debug($"Received response for unknown RPC ID {message.RpcId}, {message}");
        }
    }
    
    
    public void AddCallback(IResponseCompletionSource source, ServerMessage msg)
    { 
        var callbackData = new CallBack<ServerMessage>(source, msg);
        _callbacks.TryAdd(msg.RpcId, callbackData);
    }
    
    public void Clear()
    {
        _callbacks.Clear();
    }

}