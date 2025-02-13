using System.Collections;

namespace EasyServer.Utility;

public static class TypeExtension
{
    public static bool IsNullOrEmpty<T>(this T[]? array)
    {
        return array == null || array.Length == 0;
    }

    public static bool IsNullOrEmpty(this ICollection? list)
    {
        return list == null || list.Count == 0;
    }
}  