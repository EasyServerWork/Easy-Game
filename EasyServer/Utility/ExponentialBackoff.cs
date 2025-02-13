namespace EasyServer.Utility;

/// <summary>
/// 指数退避算法
/// </summary>
public class ExponentialBackoff
{
    private static readonly Random Random = new Random();
    private readonly int _initialDelayMilliseconds = 1000; // 初始延迟时间
    private readonly int _maxDelayMilliseconds = 32000; // 最大延迟时间
    
    private int _deltaMilliseconds = 0;   // 累计时间
    
    private int _currentAttempt = 0;
    
    public int CurrentAttempt => _currentAttempt;
    public int DeltaMilliseconds => _deltaMilliseconds;
    
    public ExponentialBackoff()
    { }
        
    public ExponentialBackoff(int initialDelayMilliseconds, int maxDelayMilliseconds)
    {
        _initialDelayMilliseconds = initialDelayMilliseconds;
        _maxDelayMilliseconds = maxDelayMilliseconds;
    }

    public TimeSpan NextDelay
    {
        get
        {
            int newValue = Interlocked.CompareExchange(ref _currentAttempt, _currentAttempt, _currentAttempt);
            int delay = Math.Min(_maxDelayMilliseconds, _initialDelayMilliseconds * (int)Math.Pow(2, newValue));
            delay += Random.Next(0, 1000); // 添加随机性避免同步问题
            Interlocked.Increment(ref _currentAttempt);
            Interlocked.Add(ref _deltaMilliseconds, delay);
            return TimeSpan.FromMilliseconds(delay);
        }
    }
    
    
    public void Reset()
    {
        Interlocked.Exchange(ref _currentAttempt, 0);
        Interlocked.Exchange(ref _deltaMilliseconds, 0);
    }
}