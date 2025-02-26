using EasyServer.Log;

namespace EasyServer.Core;

/// <summary>
/// Actor的Task调度, 任务会由这个类的对像调用 workItemGroup.Enqueue() 方法进行入队, 然后由线程池进行调度执行
/// </summary>
internal class ActorScheduler : TaskScheduler
{
    private readonly ActorWorkItemGroup _workItemGroup;
    private readonly ILogger _logger;
    
    
    public ActorScheduler(ILogger logger)
    {
        _workItemGroup = new ActorWorkItemGroup(this, logger);
        _logger = logger;
    }
    
    protected override IEnumerable<Task>? GetScheduledTasks()
    {
        return this._workItemGroup.GetTasks();
    }

    protected override void QueueTask(Task task)
    {
        _workItemGroup.Enqueue(task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        // 不进行内联，所有任务都进入队列
        return false;
    }

    internal void ExecTask(Task task)
    {
        bool done = TryExecuteTask(task);
        if (!done)
        {
            _logger.Warning($"ActorSchedule.ExecTask: Not Successful, taskId= {task.Id}, status={task.Status}");
        }
    }
    
    public void Exit()
    {
        _workItemGroup.Exit();
    }
}