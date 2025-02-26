namespace EasyServer.Core;

public class ScheduleOptions
{
    /// <summary>
    /// 任务连续执行的时间，超过这个值，会退出，并重新按排排程，让其化任何有执行的可能，防止任务饥饿
    /// </summary>
    public static readonly TimeSpan ThroughputTimeMs = TimeSpan.FromMilliseconds(100);

    
    /// <summary>
    /// 执行超时警告阈值，超过这个值，会触发警告。（Task在执行过程中，如果超过这个值，会触发警告）
    /// </summary>
    public static TimeSpan ExecuteWarningThreshold = TimeSpan.FromMilliseconds(1_000);

    /// <summary>
    /// 待决定的工作项队列大小，超过这个值，会触发警告。
    /// </summary>
    public static int MaxPendingWorkItemsLimit = 1000;
}