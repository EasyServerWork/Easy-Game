using System.Collections.Concurrent;
using System.Text;
using dotnet_etcd;
using EasyServer.Utility;
using Etcdserverpb;
using Google.Protobuf;
using Grpc.Core;
using Mvccpb;

namespace EasyServer.Registry;


[Scheme("etcd")]
public class EtcdServiceRegistry(RegistryConfig config) : IServiceRegistry
{
    // 租约时间,单位秒
    protected const int LeaseTtl = 60;
    // 操作超时时间,单位秒
    protected const int TimeOut = 5;
    // 注册方法的调用间隔，单位秒
    protected const int FuncCallInterval = 5;
    
    // 避让算法
    private ExponentialBackoff _backoff = new ();
    
    private readonly RegistryConfig _config = config;
    
    private List<IServiceListener?> _listeners = new();
    
    
    // 存放所有的值
    private ConcurrentDictionary<string, ServiceContent> _allContents = new ();
        
    // 当前进程下的key-value（在当前进程下进行注册的值才会存在于这里）
    private readonly ConcurrentDictionary<string, string> _currentValues = new ();

    private EtcdClient _client;
    

    // 根据key前缀的最近一次全局版本号
    // key与config的Prefixs是一一对应的
    private readonly ConcurrentDictionary<string, long> _revisions = new ();
    
    // 租约
    private long _leaseId;

    // 统计计算方法, key: 统计的结果在etcd的key, value: 统计方法
    private Dictionary<string, Func<string>> _funcs = new ();

    private readonly CancellationTokenSource _rootCts = new CancellationTokenSource();
    
    

    public void AddListener(IServiceListener? listener)
    {
        // 这里 _listeners 并没有采用并发类型，也没有加锁，但下面代码的写法是可以避免并发的。利用了引用赋值的原子性
        if (listener != null)
        {
            var tmpListeners = new List<IServiceListener?>(_listeners);
            tmpListeners.Add(listener);

            _listeners = tmpListeners;
        }
    }
    

    public Task RegisterAsync(string key, string value)
    {
        throw new NotImplementedException();
    }

    public Task RegisterFuncAsync(string key, Func<string> func)
    {
        throw new NotImplementedException();
    }

    
    public async Task Start(params IServiceListener[]? listeners)
    {
        // 重新组织连接字符串，因为etcd的连接需要http，config.ConnectionString是一个用逗号隔开的ip:port的字符串
        // 希望拆出来，并且每个的前面都加上http://, 写出_config.ConnectionString的拆解代码
        var etcdConnectionString = BuildConnectionString();
        _client = BuildClient(etcdConnectionString);
        
        // 添加监听器
        if (!listeners.IsNullOrEmpty())
        {
            foreach (var l in listeners!)
            {
                AddListener(l);
            }
        }
        
        foreach (var prefix in _config.Prefixs)
        {
            // 获取key的版本号
            var revision = _revisions.GetOrAdd(prefix, _ => 0L);
        }
        
        // 通知Start完成
        TaskCompletionSource<bool> notify = new TaskCompletionSource<bool>();


        await notify.Task;
    }

    public Task Stop()
    {
        throw new NotImplementedException();
    }



    private async Task MainLoop(TaskCompletionSource<bool> notify)
    {
        while (!_rootCts.IsCancellationRequested)
        {
            // 每次循环都用一个新的cts, 当发生异常时,取消当前cts,重新开始, rootCts的取消意味着关停
            using (CancellationTokenSource linkedCts =
                   CancellationTokenSource.CreateLinkedTokenSource(_rootCts.Token))
            {
                try
                {
                    // logger.Info("EtcdServiceRegistry Being InnerStart....");

                    // 构建租约
                    long leaseId = await BuildLeaseId(linkedCts);
                    // 组约绑定
                    await BindLeaseToCurrValues(linkedCts, leaseId);
                    // 同步节点数据
                    await Sync(linkedCts);
                    // 拉起监控
                    Watch(linkedCts);
                    // 拉起循环方法计算
                    CalFuncs(linkedCts);
                        
                    // 通知Start方法完成, Start方法最终不会发生阻塞
                    notify.TrySetResult(true);

                    // 在不阻塞当前循环的情况下,进行避让还原
                    BackOffReset(linkedCts);
                    // 进行续租, 每1/10的租约时间进行一次续租
                    // logger.Info("EtcdServiceRegistry LeaseKeepAlive Start....");
                    await _client.LeaseKeepAlive(linkedCts, leaseId, (LeaseTtl * 1000) / 10);
                }
                catch (OperationCanceledException e1)
                {
                    // logger.Warning("EtcdServiceRegistry insideStart Cancel: {0} ", e1.Message);
                }
                catch (Exception e2)
                {
                    // logger.Error(e2, e2.Message);
                }
                finally
                {
                    await linkedCts.CancelAsync();
                    await Task.Delay(_backoff.NextDelay);
                }
            }
        }
            
        // logger.Info("EtcdServiceRegistry Exit");
    }
    
    
    /// <summary>
    /// 构建连接字符串
    /// </summary>
    /// <returns></returns>
    private string BuildConnectionString()
    {
        string connectionString = _config.ConnectionString;
        string httpScheme = "http";
        if (_config.IsSecure)
        {
            httpScheme = "https";
        }
        if (connectionString.Contains(","))
        {
            var ipPorts = connectionString.Split(',');
            var sb = new StringBuilder();
            foreach (var ipPort in ipPorts)
            {
                sb.Append($"{httpScheme}://{ipPort},");
            }
            
            connectionString = sb.ToString().TrimEnd(',');
        }
        connectionString = $"{httpScheme}://{_config.ConnectionString}";
        return connectionString;
    }
    
    /// <summary>
    /// 构建etcd客户端
    /// </summary>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    private EtcdClient BuildClient(string connectionString)
    {
        if (_config.IsSecure)
        {
            return new EtcdClient(
                connectionString, 
                configureChannelOptions:options => options.Credentials = ChannelCredentials.SecureSsl);    
        }
        else
        {
            return new EtcdClient(
                connectionString, 
                configureChannelOptions:options => options.Credentials = ChannelCredentials.Insecure);
        }
    }
    
    private void UpdateLeaseId(long leaseId)
    {
        Interlocked.Exchange(ref _leaseId, leaseId);
    }
    
    private long GetLeaseId()
    {
        return Interlocked.Read(ref _leaseId);
    }
    
    // 构建租约
    private async Task<long> BuildLeaseId(CancellationTokenSource cts)
    {
        using (CancellationTokenSource linkedCts =
               CancellationTokenSource.CreateLinkedTokenSource(cts.Token))
        {
            // 操作etcd的超时时间 OperTimeOut
            linkedCts.CancelAfter(TimeSpan.FromSeconds(TimeOut));
            var leaseResp = await _client.LeaseGrantAsync(new LeaseGrantRequest
            {
                TTL = LeaseTtl
            }, cancellationToken:linkedCts.Token);
                
            UpdateLeaseId(leaseResp.ID);
            // logger.Info("EtcdManager BuildLeaseId, LeaseId: {0}", leaseResp.ID);
                
            return leaseResp.ID;
        }
    }
    
    /// <summary>
    /// 租约的剩余时间
    /// </summary>
    /// <param name="cts">取消源</param>
    /// <param name="leaseId">租约ID</param>
    /// <returns></returns>
    private async Task<long> RemainLeaseTtl(CancellationTokenSource cts, long leaseId)
    {
        using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token))
        {
            linkedCts.CancelAfter(TimeSpan.FromSeconds(TimeOut));
            var leaseTimeToLiveRequest = new LeaseTimeToLiveRequest { ID = leaseId };
            var leaseTimeToLiveResponse = await _client.LeaseTimeToLiveAsync(leaseTimeToLiveRequest, cancellationToken: linkedCts.Token);
                
            return leaseTimeToLiveResponse.TTL;
        }
    }
    
    
    /// <summary>
    /// 检查租约是否在合理范围内
    /// </summary>
    /// <param name="cts"></param>
    /// <param name="leaseId"></param>
    /// <returns></returns>
    private async Task<bool> CheckTtl(CancellationTokenSource cts, long leaseId)
    {
        // 检查ttl是否在合理范围内
        var ttl = await RemainLeaseTtl(cts, leaseId);
        // logger.Info("EtcdManager CheckTTL, LeaseId: {0}, TTL: {1}", leaseId, ttl);
        if (ttl <= TimeOut)
        {
            // logger.Error("EtcdManager CheckTTL, Lease be about to expire LeaseId: {0}, TTL: {1}", leaseId, ttl);
            return false;
        }

        return true;
    }
    
    
    /// <summary>
    /// 用于原子的更新修正版本号(只有大于的情况下才能更新正常)
    /// </summary>
    /// <param name="location"></param>
    /// <param name="newValue"></param>
    /// <returns></returns>
    private bool UpdateIfGreater(ref long location, long newValue)
    {
        long initialValue;
        long computedValue;
        
        do
        {
            initialValue = Interlocked.Read(ref location);
            if (newValue <= initialValue)
            {
                // 新值比当前值小, 不更新
                return false;
            }

            // 另一个线程改值了,逻缉重入再次计算
            computedValue = Interlocked.CompareExchange(ref location, newValue, initialValue);
        }
        while (initialValue != computedValue); 

        return true; 
    }
    
    // 根据前缀更新全局修定版本号。
    private void UpdateRevision(string prefix, long revision)
    {
        long tmpRevision = 0;
            
        if (_revisions.TryGetValue(prefix, out tmpRevision))
        {
            // 前缀直接命中了
            UpdateIfGreater(ref tmpRevision, revision);
            _revisions[prefix] = tmpRevision;
        }
        else
        {
            // 从_revisions里找key是prefix前缀的项，然后进行更新
            bool found = false;
            foreach (var (key, value) in _revisions)
            {
                if (prefix.StartsWith(key))
                {
                    tmpRevision = value;
                    UpdateIfGreater(ref tmpRevision, revision);
                    _revisions[key] = tmpRevision;
                        
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                _revisions[prefix] = revision;
            }
        }
    }
    
    
    /// <summary>
    /// 更新key
    /// </summary>
    /// <param name="cts"></param>
    /// <param name="leaseId"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <exception cref="Exception"></exception>
    private async Task Update(CancellationTokenSource cts, long leaseId, string key, string value)
    {
        // 检查当前进程是否有对key进行过注册
        if (!_currentValues.ContainsKey(key))
        {
            throw new Exception($"EtcdServiceRegistry Update, Key not exists, Key: {key}");
        }

        if (!await CheckTtl(cts, leaseId))
        {
            throw new Exception($"EtcdServiceRegistry Update, Lease be about to expire leaseId: {leaseId}, key: {key}");
        }
            
        // logger.Info("EtcdServiceRegistry Update, LeaseId: {0}, Key: {1}, Value: {2}", leaseId, key, value);
        using (CancellationTokenSource linkedCts =
               CancellationTokenSource.CreateLinkedTokenSource(cts.Token))
        {
            linkedCts.CancelAfter(TimeSpan.FromSeconds(TimeOut));
                
            var putRequest = new PutRequest
            {
                Key = ByteString.CopyFromUtf8(key),
                Value = ByteString.CopyFromUtf8(value),
                Lease = leaseId
            };
                
            var resp = await _client.PutAsync(putRequest, cancellationToken:linkedCts.Token);
            UpdateRevision(key, resp.Header.Revision);
                
            _currentValues[key] = value;
        }
    }
    
    
    /// <summary>
    /// 为当前进程已注册的key进行租约绑定
    /// </summary>
    /// <param name="cts"></param>
    /// <param name="leaseId"></param>
    private async Task BindLeaseToCurrValues(CancellationTokenSource cts, long leaseId)
    {
        // logger.Info("EtcdServiceRegistry UpdateValues Start....leaseId: {0}, CurrValues.Len: {1}", leaseId, _currentValues.Count);
            
        foreach (var (key, value) in this._currentValues)
        {
            await this.Update(cts, leaseId, key, value);
        }
    }
    
    
    /// <summary>
    /// 获取指定前缀的全局修正版本号
    /// </summary>
    /// <param name="prefix"></param>
    /// <returns></returns>
    private long GetRevision(string prefix)
    {
        long tmpRevision = 0;
        if (_revisions.TryGetValue(prefix, out tmpRevision))
        {
            return tmpRevision;
        }
        else
        {
            foreach (var (key, value) in _revisions)
            {
                if (prefix.StartsWith(key))
                {
                    return value;
                }
            }
        }
        return 0;
    }

    
    private ServiceContent BuildServiceContent(KeyValue kv)
    {
        var value = new ServiceContent(kv.Key.ToStringUtf8(), kv.Value.ToStringUtf8(), kv.Version);
        return value;
    }
    
    
    // 获取指定前缀的所有key
    private async Task<Dictionary<string, ServiceContent>> GetKeysByPrefix(CancellationTokenSource cts, string prefix)
    {
        using (CancellationTokenSource linkedCts =
               CancellationTokenSource.CreateLinkedTokenSource(cts.Token))
        {
            linkedCts.CancelAfter(TimeSpan.FromSeconds(TimeOut));
                
            // 构建请求, Revison的值一定不会是0, 同步前一定是有一次etcd的操作,每次etcd的操作都会把Revison同步过来
            string rangeEnd = EtcdClient.GetRangeEnd(prefix);
            RangeRequest req = new RangeRequest
            {
                Key = EtcdClient.GetStringByteForRangeRequests(prefix),
                RangeEnd = ByteString.CopyFromUtf8(rangeEnd),
                Revision = GetRevision(prefix)    // 这个值比较关键,能解决同步数据和拉起监控之间不同步
            };
                
            var result = new Dictionary<string, ServiceContent>();
                
            var rangeResponse = await _client.GetAsync(req, cancellationToken: linkedCts.Token);
            UpdateRevision(prefix, rangeResponse.Header.Revision);
                
            foreach (var kv in rangeResponse.Kvs)
            {
                result[kv.Key.ToStringUtf8()] = BuildServiceContent(kv);
            }
            return result;
        }
    }
    
    
    /// <summary>
    /// 同步所有设定的前缀
    /// </summary>
    /// <param name="cts"></param>
    private async Task Sync(CancellationTokenSource cts)
    {
        var prefixs = _config.Prefixs;
            
        var etcdValues = new ConcurrentDictionary<string, ServiceContent>();
        foreach (var prefix in prefixs)
        {
            var result = await this.GetKeysByPrefix(cts, prefix);
            foreach (var kv in result)
            {
                etcdValues[kv.Key] = kv.Value;
            }
        }
            
        if (etcdValues.Count > 0)
        {
            var values = etcdValues.Values.ToArray();
            Interlocked.Exchange(ref _allContents, etcdValues);   // 全部替换
            // logger.Info("EtcdServiceRegistry Sync, Len: {0} Values: {1}", values.Length, JsonConvert.SerializeObject(values));
            DispatchEvent(EventType.Sync, values);
        }
    }
    
    
    /// <summary>
    /// 构建监控请求，把所有前缀都构建出来
    /// </summary>
    /// <returns></returns>
    private WatchRequest[] BuildWatchRequests()
    {
        List<WatchRequest> requests = new List<WatchRequest>();
            
        var prefixs = _config.Prefixs;
            
        foreach (var prefix in prefixs)
        {
            // logger.Info("EtcdServiceRegistry BuildWatchRequests, Prefix: {0}", prefix);
            var request = new WatchRequest
            {
                CreateRequest = new WatchCreateRequest
                {
                    Key = EtcdClient.GetStringByteForRangeRequests(prefix),
                    RangeEnd = ByteString.CopyFromUtf8(EtcdClient.GetRangeEnd(prefix)),
                    PrevKv = true,
                    StartRevision = GetRevision(prefix) + 1 // 这个很关键,能解决同步数据和拉起监控之间的不同步
                }
            };
            requests.Add(request);
        } 
        return requests.ToArray();
    }
    
    
    // 拉起监控, 监控会在一个协程里一直运行, 直到发生错误, 错误会调用取消, 以便让主循环(Start)重新开始
    private async Task Watch(CancellationTokenSource cts)
    {
        if (!cts.IsCancellationRequested)
        {
            try
            {
                // logger.Info("EtcdServiceRegistry Watch Start....");
                var requests = BuildWatchRequests();

                if (requests.IsNullOrEmpty())
                {
                    // logger.Warning("EtcdServiceRegistry Watch, requests is null");
                    return;
                }
                    
                await _client.WatchAsync(requests, WatchCallback, cancellationToken: cts.Token);
            }
            catch (RpcException e1) when (e1.Status.StatusCode == StatusCode.Cancelled)
            {
                // logger.Warning("EtcdServiceRegistry Watch Cancel: {0} ", e1.Message);
            }
            catch (Exception e2)
            {
                // logger.Error(e2, e2.Message);
            }
            finally
            {
                await cts.CancelAsync();
                // logger.Info("EtcdManager Watch End....");
            }
        }
    }
    
    
    /// <summary>
    /// 监控回调，会保障同一个key的事件是有序的。
    /// </summary>
    /// <param name="response"></param>
    private void WatchCallback(WatchResponse response)
    {
        // logger.Info("Watch response received. {0}, events.len: {1}", response.WatchId, response.Events.Count);
        
        // 对本地Revision进行更新
        foreach (var e in response.Events)
        {
            string key = e.Kv.Key.ToStringUtf8();
            UpdateRevision(key, response.Header.Revision);
            // logger.Info("EtcServiceRegistryr WatchCallback, Key: {0},  Revision: {1}", key,response.Header.Revision);
        }
        
        
        foreach (var e in response.Events)
        {
            string key = e.Kv.Key.ToStringUtf8();
            // 赋值给临时变量,避免多线程操作
            var tmpDic = _allContents;
            ServiceContent[] etcdValues = null;
            switch (e.Type)
            {
                case Event.Types.EventType.Put:
                    // 发生Put事件,进行比对,如果没有发生变,则事件不进行广播
                    var etcdValue = BuildServiceContent(e.Kv);
                    
                    if (!tmpDic.TryGetValue(key, out var tmpValue))
                    {
                        // 没有找到,说明是新增
                        etcdValues = new ServiceContent[] { etcdValue };
                        tmpDic[key] = etcdValue;
                        DispatchEvent(EventType.Change, etcdValues);
                    }
                    else
                    {
                        // 找到了,说明更新,更新操作需要进行一些比对,如果发现有变化才广播事件
                        
                        // 检查是否有变化
                        if (tmpValue.Version != etcdValue.Version)
                        {
                            etcdValues = new ServiceContent[] { etcdValue };
                            tmpDic[key] = etcdValue;
                            DispatchEvent(EventType.Change, etcdValues);
                        } 
                    }
                    break;
                case Event.Types.EventType.Delete:
                    tmpDic.TryRemove(key, out _);
                    DispatchEvent(EventType.Delete, new ServiceContent[] { BuildServiceContent(e.PrevKv) });
                    break;
            }
            // logger.Info("EtcdServiceRegistry WatchCallback, Key: {0}, Type: {1}, event: {2}", 
            //     key, e.Type.ToString(), etcdValues.IsNullOrEmpty()?"Empty":JsonConvert.SerializeObject(etcdValues));
        }
    }
    
    
    /// <summary>
    /// 调用注册方法
    /// </summary>
    /// <param name="cts"></param>
    private async Task CalFuncs(CancellationTokenSource cts)
    {
        // logger.Info("EtcdServiceRegistry CalFuncs Start....");
        
        try
        {
            while (!cts.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(FuncCallInterval), cts.Token);

                // var session = Utils.GenerateUniqueId(16);
                var tmpFuncs = _funcs;
                
                // logger.Info("EtcdServiceRegistry CalFuncs Running....Session: {0}, Funs.Len: {1}", session, tmpFuncs.Count);
                if (tmpFuncs.Count > 0)
                {
                    foreach (var (key, func) in tmpFuncs)
                    {
                        var leaseId = GetLeaseId();
                        
                        string v = null;
                        try
                        {
                            // func可能会抛出异常,所以需要try-catch, CalStat是在主循环里的, 除了etcd本身的错误外，其他异常尽量不要影响主循环
                            v = func();
                        }
                        catch (Exception e)
                        {
                            // logger.Error("EtcdServiceRegistry CalFuncs Err, Session: {0}, LeaseId: {1} Key: {2}, Error: {3}", session, leaseId, key, e.Message);
                        }

                        if (v != null)
                        {
                            // logger.Info("EtcdServiceRegistry CalFuncs, Session: {0}, LeaseId: {1} Key: {2}, Value: {3}", session, leaseId, key, v);
                            await this.Update(cts, leaseId, key, v);   
                        }
                    }
                }
            }
        }
        catch (RpcException e) when (e.Status.StatusCode == StatusCode.Cancelled)
        {
            // logger.Warning("EtcdServiceRegistry CalFuncs Cancel: {0} ", e.Message);
        }
        catch (OperationCanceledException e)
        {
            // logger.Warning("EtcdServiceRegistry CalFuncs Cancel: {0} ", e.Message);
        }
        catch (Exception e)
        {
            // logger.Error(e, e.Message);
        }
        finally
        {
            await cts.CancelAsync();
        }
    }
    
    
    // 重置backOff, 方法无论怎么调用都不应该让异常抛出去
    private async Task BackOffReset(CancellationTokenSource cts)
    {
        try
        {
            // 等待一段时间后,如果发现没有取消,则重置backoff
            await Task.Delay(TimeSpan.FromSeconds(TimeOut));
            if (!cts.IsCancellationRequested)
            {
                _backoff.Reset();
            }
        }
        catch (Exception e)
        {
            // logger.Error(e, e.Message);
            _backoff.Reset();
        }
    }
    
    
    /// <summary>
    /// 分发事件
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="values"></param>
    private void DispatchEvent(EventType eventType, ServiceContent[] values)
    {
        if (values.IsNullOrEmpty())
        {
            // logger.Warning("EtcdRegistry DispatchEvent, Values is null");
        }
        
        var tmpListeners = _listeners;
        
        foreach (var etcdListener in tmpListeners)
        {
            List<ServiceContent> valueList = new ();
            
            var prefixs = etcdListener?.Prefixes();
            if (prefixs.IsNullOrEmpty())
            {
                continue;
            }
            
            foreach (var etcdValue in values)
            {
                foreach (var prefix in prefixs)
                {
                    if (etcdValue.Key.StartsWith(prefix))
                    {
                        valueList.Add(etcdValue);
                        break;
                    }
                }
            }
            
            // logger.Info("EtcdServiceRegistry DispatchEvent, ListenerType: {0}, EventType: {1}, Values: {2}", 
            //     etcdListener.GetType().Name, eventType.ToString(), JsonConvert.SerializeObject(valueList));

            // 每个监听器尽量不影响其他监听器，用try-catch包起来，避免异常影响其他监听器
            try
            {
                if (valueList.Count > 0)
                {
                    var etcdEvent = new EventData(eventType, valueList.ToArray());
                    etcdListener?.OnEvent(etcdEvent);
                }
            }
            catch (Exception e)
            {
                // logger.Error(e, "EtcdServiceRegistry DispatchEvent Error ListenerType: {0}, EventType: {1}", 
                //     etcdListener.GetType().Name, eventType.ToString());
            }
        }
    }
    
}