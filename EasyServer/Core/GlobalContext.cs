namespace EasyServer.Core;


/// <summary>
///  全局上下文
/// </summary>
public static class GlobalContext
{
    /// <summary>
    /// 热修复管理
    /// </summary>
    private static HotfixManager _hotfix;
    
    public static HotfixManager Hotfix { get; set; }
    
}