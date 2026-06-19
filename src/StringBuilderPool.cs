using System.Collections.Concurrent;
using System.Text;

namespace MT.HexDump;

public static class StringBuilderPool
{
    private static readonly ConcurrentBag<StringBuilder> _pool = new();

    public static StringBuilder Rent(int capacity)
    {
        if (_pool.TryTake(out var sb))
        {
            sb.Clear();
            return sb;
        }
        return new(capacity);
    }

    public static void Return(StringBuilder sb)
    {
        if (sb.Capacity > 4096)
        {
            return;
        }
        _pool.Add(sb);
    }
}
