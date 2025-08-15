using System.Diagnostics;
using System.Text;

namespace MT.HexDump;

public static class HexDumper
{
    [Conditional("DEBUG")]
    public static void DebugPrint(string msg, ConsoleColor foreground = ConsoleColor.Red)
    {
        var c = Console.ForegroundColor;
        Console.ForegroundColor = foreground;
        Console.Error.WriteLine(msg);
        Console.ForegroundColor = c;
    }

    public static IEnumerable<CharCollectionRow> HexDump(ReadOnlyMemory<byte> data, long offset = 0, int length = 0)
    {
        return HexDump(data, Encoding.UTF8, offset, length);
    }
    public static IEnumerable<CharCollectionRow> HexDump(ReadOnlyMemory<byte> data, Encoding encoding, long offset = 0, int length = 0)
    {
        if (data.Length < offset)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset value too large for data length {data.Length}.");
        }
        if (offset > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset must be smaller than int ({int.MaxValue})");
        }
        var targetData = length > 0 && data.Length > offset + length
            ? data.Slice((int)offset, length)
            : data.Slice((int)offset);
        var enc = Encoding.GetEncoding(encoding.CodePage, EncoderFallback.ReplacementFallback, new IgnoreFallback());
        CharCollectionRow charDatas = new();
        DebugPrint($"All bytes = [{string.Join(' ', targetData.ToArray().Select(static b => $"{b:X2}"))}]", ConsoleColor.Green);
        foreach (var charData in HexDumpCore(targetData, enc, offset))
        {
            var col = charData.Col;
            charDatas.Set(charData);
            if (col == 0x0F)
            {
                yield return charDatas;
                charDatas = new();
            }
        }
        if (!charDatas.IsEmpty)
        {
            yield return charDatas;
        }
    }

    private static IEnumerable<CharData> HexDumpCore(ReadOnlyMemory<byte> data, Encoding enc, long offset = 0)
    {
        int p = 0;
        while (p < data.Length)
        {
            var bytes = data.Span.Slice(p, Math.Min(4, data.Length - p)).ToArray();
            char[] chars = new char[2];
            enc.TryGetChars(bytes, chars, out _);
            if (chars[0] <= 0xFF)
            {
                DebugPrint($"p={p:X8}: char[0]=0x{(int)chars[0]:X2} [0x00 - 0xFF] bytes=[{string.Join(' ', bytes.Select(static b => $"{b:X2}"))}]", ConsoleColor.Blue);
                yield return new CharData(bytes[0], offset + p++, (int)chars[0]);
            }
            else if (char.IsSurrogatePair(chars[0], chars[1]))
            {
                DebugPrint($"p={p:X8}: char=[0x{(int)chars[0]:X2}, 0x{(int)chars[1]:X2}] [SurrogatePair] bytes=[{string.Join(' ', bytes.Select(static b => $"{b:X2}"))}]", ConsoleColor.Blue);
                yield return new CharData(bytes[0], offset + p++, char.ConvertToUtf32(chars[0], chars[1]));
                for (var j = 1; j < bytes.Length; j++)
                {
                    yield return new CharData(bytes[j], offset + p++);
                }
            }
            else
            {
                int byteCount = enc.GetByteCount(chars[..1]);
                DebugPrint($"p={p:X8}: char[0]=0x{(int)chars[0]:X2} [> 0xFF] bytes=[{string.Join(' ', bytes.Select(static b => $"{b:X2}"))}]", ConsoleColor.Blue);
                yield return new CharData(bytes[0], offset + p++, (int)chars[0]);
                for (var j = 1; j < byteCount; j++)
                {
                    yield return new CharData(bytes[j], offset + p++);
                }
            }
        }
    }
}
