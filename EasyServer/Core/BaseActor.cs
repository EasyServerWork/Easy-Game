using EasyServer.Log;
using EasyServer.Utility;

namespace EasyServer.Core;


public static class ActorState
{ 
    public static int Init = 0; // 初始化完成
    public static int Running = 1; // 运行中
    public static int Exiting = 2; // 退出中,未完成
    public static int Exited = 3; // 已退出
}


/// <summary>
/// 所有Actor的基类
/// </summary>
public abstract class BaseActor
{
    public ILogger Logger { get; private set; }
    
    private ulong _curRpcId = 0;
    private readonly ResponseHandler _responseHandler;
    
    
    private readonly ActorScheduler _scheduler;
    private readonly SingleWaiterAutoResetEvent _workSignal = new() { RunContinuationsAsynchronously = true };

    
    private readonly Queue<(ServerMessage Message, Stopwatch QueuedTime)> _waitingRequests = new();
    private readonly Dictionary<ServerMessage, Stopwatch> _runningRequests = new();
    
    internal ActorScheduler Scheduler { get { return _scheduler; } }

    private int _state;
    
    protected int State { get { return _state; } }

    public uint ServiceId { get; private set; }
    protected string? ServiceName { get; private set; }

    // private ServiceSource _source;


    public BaseActor()
    {
        Logger = LoggerManager.CreateLogger(GetType());
        _responseHandler = new ResponseHandler(Logger);
    }
    
    /// <summary>
    /// 消息处理循环，每一个Actor会有一个，这里接收的是Actor的入口方法消息
    /// </summary>
    private async Task Loop()
    {
        // 消息处理循环，每一个Actor会有一个，这里接收的是Actor的入口方法消息，
        // 永远不会退出
        // 借助signal来实现有消息时唤醒,无消息时让出资源并等待
        // TODO 服务退出时是否能正确释放资源?
        // _state = ServiceState.Running;
        // while (true)
        // {
        //     try
        //     {
        //         ProcessRequest();
        //         await _workSignal.WaitAsync();
        //     }
        //     catch (Exception ex)
        //     {
        //         Logger.Error(ex, "MessageLoop break......");
        //         // TODO 不可能走到这里 记录Fatal日志
        //     }
        // }
    }
}