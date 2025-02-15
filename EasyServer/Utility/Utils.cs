namespace EasyServer.Utility;

public static class Utils
{
    /// <summary>
    /// 生成一个唯一Id
    /// </summary>
    /// <param name="length">指定ID的长度</param>
    /// <returns></returns>
    public static string GenerateUniqueId(int length)
    {
        // 创建一个新的 Guid
        Guid guid = Guid.NewGuid();
        
        // 将Guid转换为一个字符串并移除 '-' 符号
        string guidString = guid.ToString("N");
        
        // 如果需要的长度超过传统Guid的长度则调整
        if (length > guidString.Length)
        {
            length = guidString.Length;
        }

        // 截取前length个字符
        return guidString.Substring(0, length);
    }
}