using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EasyServer.Log;

namespace EasyServer.Core;

internal class ActorWorkItemGroup : IThreadPoolWorkItem
{
    private enum WorkGroupStatus
    {
        Waiting = 0, // 空闲状态
        Runnable = 1, // 可以运行
        Running = 2, // 正在运行
        Exited = 3, // 退出
    }
    
    /// <summary>
    /// 任务队列，所有在Actor方法里产生的Task都会放置到这个队列里，并通过IWorkItem进行有序化执行
    /// 切记这个队列是Actor方法里里产生的Task，对于Actor方法并不会压入到这个队列。
    /// </summary>
    private readonly Queue<Task> _tasks;
    private WorkGroupStatus _state;
    private readonly object _lockable;
    private readonly ILogger _logger;
    private int _taskCount => _tasks.Count;

    private long _lastLongQueueWarningTimestamp;
    
    /// <summary>
    /// 任务调度器，基于System.Threading.Tasks.TaskScheduler基类的实现, 
    /// </summary>
    internal ActorScheduler TaskScheduler { get; }
    
    internal ActorWorkItemGroup(ActorScheduler taskScheduler, ILogger logger)
    {
        _tasks = new Queue<Task>();
        _state = WorkGroupStatus.Waiting;
        _lockable = new object();
        TaskScheduler = taskScheduler;
        _logger = logger;
    }
    
    internal IEnumerable<Task> GetTasks()
    {
        foreach (var task in this._tasks)
        {
            yield return task;
        }
    }
    
    
    /// <summary>
    /// 产生的Task入到队列里，让线程池可以有序处理，周时触发线程池排程
    /// 以下情况会触发Task入队
    /// 1. MessageLoop循环信号触发时
    /// 2. 业务逻辑await调用完成后,剩余逻辑会以新任务的方式入队
    /// </summary>
    /// <param name="task"></param>
    public void Enqueue(Task task)
    {
        lock (_lockable)
        {
            // 状态退出时，直接返回，不再处理任何task
            if (_state >= WorkGroupStatus.Exited)
                return;
                
            int count = _tasks.Count;
            _tasks.Enqueue(task);

            int maxPendingItemsLimit = ScheduleOptions.MaxPendingWorkItemsLimit;
            if (maxPendingItemsLimit > 0 && count > maxPendingItemsLimit)
            {
                var now = Environment.TickCount64;
                if (now > _lastLongQueueWarningTimestamp + 10_000)
                {
                    _logger.Warning($"Too many tasks in the queue, Current count:{count}, Warning threshold: {maxPendingItemsLimit}");
                }

                _lastLongQueueWarningTimestamp = now;
            }

            if (_state != WorkGroupStatus.Waiting) 
                return;
                
            _state = WorkGroupStatus.Runnable;
            Schedule();          
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public Task Dequeue()
    {
        Task task = null!;
        lock (_lockable)
        {
            if (_state >= WorkGroupStatus.Exited)
                return task;

            _state = WorkGroupStatus.Running;
            if (_taskCount > 0)
            {
                task = _tasks.Dequeue();
            }
        }
        return task;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Schedule()
    {
        ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
    }

    /// <summary>
    /// 从队列里取出Task，并执行，如果队列里还有Task，则继续执行，否则退出循环，重新排程
    /// </summary>
    public void Execute()
    {
        var warningDurationMs = ScheduleOptions.ExecuteWarningThreshold.TotalMilliseconds;
        var throughputTimeMs = ScheduleOptions.ThroughputTimeMs.TotalMilliseconds;
        try
        {
            long loopStart, taskStart, taskEnd;
            loopStart = taskStart = taskEnd = Environment.TickCount64;
            do
            {
                Task task = Dequeue();
                if (task == null)
                {
                    break;
                }
                
                try
                {
                    TaskScheduler.ExecTask(task);      
                }
                finally
                {
                    taskEnd = Environment.TickCount64;
                    var taskDurationMs = taskEnd - taskStart;
                    taskStart = taskEnd;
                    if (taskDurationMs > warningDurationMs)
                    {
                        _logger.Warning($"Single task running time is too long, running time: {taskDurationMs}, " +
                                        $"threshold: {warningDurationMs}, Running on thread {Thread.CurrentThread.ManagedThreadId.ToString()}");
                    }
                }
            }
            // 没设定吞吐时间或吞吐整个连续运行时间小于吞吐时间时，继续执行，否则退出循环，重新排程
            while (throughputTimeMs <= 0 || taskEnd - loopStart < throughputTimeMs);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }
        finally
        {
            lock (_lockable)
            {
                if (_state < WorkGroupStatus.Exited)
                {
                    if (_taskCount > 0)
                    {
                        _state = WorkGroupStatus.Runnable;
                        Schedule();
                    }
                    else
                    {
                        _state = WorkGroupStatus.Waiting;
                    }
                }
            }
        }
    }
    
    public void Exit()
    {
        lock (_lockable)
        {
            _state = WorkGroupStatus.Exited;
        }
    }
}