namespace EasyServer.Core;

public struct CustomKey
{
    public string StringKey { get; }
    public long LongKey { get; }

    public CustomKey(string stringKey)
    {
        StringKey = stringKey;
        LongKey = 0;
    }

    public CustomKey(long longKey)
    {
        LongKey = longKey;
        StringKey = null;
    }

    public override bool Equals(object obj)
    {
        if (obj is CustomKey other)
        {
            return StringKey == other.StringKey && LongKey == other.LongKey;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return StringKey != null ? StringKey.GetHashCode() : LongKey.GetHashCode();
    }
}