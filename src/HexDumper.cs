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

    public static IEnumerable<CharCollectionRow> HexDump(Stream stream, Encoding encoding, long offset = 0, int length = 0)
    {
        CharCollectionRow charDatas = new();
        foreach (var charData in HexDumpStream(stream, encoding, offset, length))
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

    public static IEnumerable<CharData> HexDumpStream(Stream stream, Encoding encoding, long offset = 0, int length = 0)
    {
        const int BUFFER_LENGTH = 1024;
        var enc = Encoding.GetEncoding(encoding.CodePage, EncoderFallback.ReplacementFallback, new IgnoreFallback());
        var buf = new byte[length > 0 ? Math.Min(BUFFER_LENGTH, length) : BUFFER_LENGTH];
        long totalReadBytes = offset;
        int remainingBytes;
        int readBytes;
        if (stream.CanSeek)
        {
            stream.Seek(offset, SeekOrigin.Begin);
        }
        else if (stream.Position < offset)
        {
            var seekCount = (offset - stream.Position) / BUFFER_LENGTH;
            remainingBytes = (int)(offset % BUFFER_LENGTH);
            for (var i = 0; i < seekCount; i++)
            {
                _ = stream.Read(buf);
            }
            if (remainingBytes > 0)
            {
                _ = stream.Read(buf, 0, remainingBytes);
            }
        }

        CharData c;
        var charDataQueue = new Queue<CharData>(3);
        int remainingQueueCount = charDataQueue.Count;
        remainingBytes = length > 0 ? length : int.MaxValue;
        while ((readBytes = stream.Read(buf, remainingQueueCount, Math.Min(buf.Length - remainingQueueCount, remainingBytes))) > 0)
        {
            for (var i = 0; i < remainingQueueCount; i++)
            {
                buf[i] = charDataQueue.Dequeue().B;
            }

            foreach (var charData in HexDumpCore(buf[..(remainingQueueCount + readBytes)], enc, totalReadBytes - remainingQueueCount))
            {
                if (charData.CodePoint is >= 0 and <= 0xFF)
                {
                    charDataQueue.Enqueue(charData);
                    while (charDataQueue.Count > 3)
                    {
                        yield return charDataQueue.Dequeue();
                    }
                }
                else
                {
                    while (charDataQueue.TryDequeue(out c))
                    {
                        yield return c;
                    }
                    yield return charData;
                }
            }
            remainingBytes -= readBytes;
            if (remainingBytes <= 0)
            {
                break;
            }
            totalReadBytes += readBytes;
            remainingQueueCount = charDataQueue.Count;
        }
        while (charDataQueue.TryDequeue(out c))
        {
            yield return c;
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
