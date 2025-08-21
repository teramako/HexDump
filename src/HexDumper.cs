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
        long position = offset;
        var enc = Encoding.GetEncoding(encoding.CodePage, EncoderFallback.ReplacementFallback, new TopBytesFallback());
        CharCollectionRow charDatas = new(position);
        DebugPrint($"All bytes = [{string.Join(' ', targetData.ToArray().Select(static b => $"{b:X2}"))}]", ConsoleColor.Green);
        foreach (var charData in HexDumpCore(targetData, enc))
        {
            charDatas.Set(position, charData);
            if ((position & 0x0F) == 0x0F)
            {
                yield return charDatas;
                charDatas = new(position + 1);
            }
            position++;
        }
        if (charDatas is not null && !charDatas.IsEmpty)
        {
            yield return charDatas;
        }
    }

    public static IEnumerable<CharCollectionRow> HexDump(Stream stream, Encoding encoding, long offset = 0, int length = 0)
    {
        long position = offset;
        CharCollectionRow charDatas = new(position);
        foreach (var charData in HexDumpStream(stream, encoding, offset, length))
        {
            charDatas.Set(position, charData);
            if ((position & 0x0F) == 0x0F)
            {
                yield return charDatas;
                charDatas = new(position + 1);
            }
            position++;
        }
        if (charDatas is not null && !charDatas.IsEmpty)
        {
            yield return charDatas;
        }
    }

    public static IEnumerable<CharData> HexDumpStream(Stream stream, Encoding encoding, long offset = 0, int length = 0)
    {
        const int BUFFER_LENGTH = 1024;
        var enc = Encoding.GetEncoding(encoding.CodePage, EncoderFallback.ReplacementFallback, new TopBytesFallback());
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
        else
        {
            throw new InvalidOperationException($"Could not seek to {offset} (current position: {stream.Position})");
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

            foreach (var charData in HexDumpCore(buf[..(remainingQueueCount + readBytes)], enc))
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

    private static IEnumerable<CharData> HexDumpCore(ReadOnlyMemory<byte> data, Encoding enc)
    {
        var fb = ((TopBytesFallback)enc.DecoderFallback).FallbackBuffer;
        int p = 0;
        char[] chars = new char[5];
        int charsWritten;
        while (p < data.Length)
        {
            var bytes = data.Span.Slice(p, Math.Min(4, data.Length - p)).ToArray();
            var byteIndex = 0;
            // var str = enc.GetString(bytes);
            if (!enc.TryGetChars(bytes, chars, out charsWritten))
            {
                throw new DecoderFallbackException($"", bytes, 0);
            }
            if (fb.HasFallbackChars)
            {
                foreach (var fbCharData in fb.GetFallbackChars())
                {
                    DebugPrint($"p={p:X8}: (Fallback) {fbCharData}");
                    yield return fbCharData;
                    p++;
                    byteIndex++;
                }
            }
            for (var i = 0; i < charsWritten; i++)
            {
                string rune;
                if (i + 1 < charsWritten && char.IsSurrogatePair(chars[i], chars[i + 1]))
                {
                    rune = char.ConvertFromUtf32(char.ConvertToUtf32(chars[i], chars[i + 1]));
                    i++;
                }
                else
                {
                    rune = char.ConvertFromUtf32((int)chars[i]);
                }
                byte byteCount = (byte)enc.GetByteCount(rune);
                CharData charData = new(bytes[byteIndex], char.ConvertToUtf32(rune, 0), true);
                DebugPrint($"p={p:X8}: {charData}", ConsoleColor.Blue);
                yield return charData;
                for (var j = 1; j < byteCount; j++)
                {
                    yield return new CharData(bytes[byteIndex + j], -j, true);
                }
                p += byteCount;
                byteIndex += byteCount;
            }
        }
    }
}
