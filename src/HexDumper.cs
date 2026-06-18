using System.Collections.Concurrent;
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

    public static IEnumerable<CharData> HexDump(ReadOnlyMemory<byte> data,
                                                long offset = 0,
                                                int length = 0,
                                                ColorType colorType = ColorType.None)
    {
        return HexDump(data, Encoding.UTF8, offset, length, colorType);
    }
    /// <summary>
    /// バイトデータのダンプを行う
    /// </summary>
    /// <param name="data">バイトデータ</param>
    /// <param name="encoding">バイトデータのデコードを試行する文字エンコーディング</param>
    /// <param name="offset">ダンプを開始する位置</param>
    /// <param name="length">ダンプを行う開始位置からの長さ</param>
    /// <param name="colorType">配色設定</param>
    public static IEnumerable<CharData> HexDump(ReadOnlyMemory<byte> data,
                                                Encoding encoding,
                                                long offset = 0,
                                                int length = 0,
                                                ColorType colorType = ColorType.None)
    {
        Config config = (Config)Config.Default.Clone();
        config.Encoding = encoding;
        config.ColorType = colorType;
        return HexDump(data, config, offset, length);
    }

    /// <inheritdoc cref="HexDump(ReadOnlyMemory{byte}, Encoding, long, int, ColorType)"/>
    /// <param name="config">HexDump用の各種設定オブジェクト</param>
    public static IEnumerable<CharData> HexDump(ReadOnlyMemory<byte> data,
                                                Config config,
                                                long offset = 0,
                                                int length = 0)
    {
        if (data.Length < offset)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset value too large for data length {data.Length}.");
        }
        if (offset > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset must be smaller than int ({int.MaxValue})");
        }
        var encoding = Encoding.GetEncoding(config.Encoding.CodePage, EncoderFallback.ReplacementFallback, new TopBytesFallback());
        var fallbackBuffer = ((TopBytesFallback)encoding.DecoderFallback).FallbackBuffer;

        ConcurrentQueue<CharData> charDataQueue = [];
        AutoResetEvent queueEvent = new(false);
        bool completed = false;


        var dumpTask = Task.Run(DumpTask);

        while (!(completed && charDataQueue.IsEmpty))
        {
            while (charDataQueue.TryDequeue(out var charData))
            {
                yield return charData;
            }

            if (completed)
                break;
            else
                queueEvent.WaitOne();
        }

        dumpTask.Wait();

        void Emit(long p, CharData cd)
        {
            if (p < 0)
            {
                queueEvent.Set();
                completed = true;
                return;
            }
            charDataQueue.Enqueue(cd);
            queueEvent.Set();
        }
        void DumpTask()
        {
            var targetData = length > 0 && data.Length > offset + length
                ? data.Span.Slice((int)offset, length)
                : data.Span.Slice((int)offset);
            DebugPrint($"All bytes = [{string.Join(' ', targetData.ToArray().Select(static b => $"{b:X2}"))}]", ConsoleColor.Green);
            HexDumpCore(targetData, encoding, fallbackBuffer, offset, Emit);
            if (fallbackBuffer.HasFallbackChars)
            {
                var position = targetData.Length - fallbackBuffer.Remaining;
                var i = 0;
                foreach (var b in fallbackBuffer.GetFallbackBytes())
                {
                    Emit(position + i, new(b, (char)b, CharType.Binary));
                    i++;
                }
            }
            // end signal
            Emit(-1, default);
        }
    }

    /// <summary>
    /// <paramref name="stream"/> のダンプを行う
    /// </summary>
    /// <param name="stream">ダンプ対象のストリーム</param>
    /// <param name="encoding">バイトデータのデコードを試行する文字エンコーディング</param>
    /// <param name="offset">ダンプを開始する位置</param>
    /// <param name="length">ダンプを行う開始位置からの長さ</param>
    /// <param name="colorType">配色設定</param>
    public static IEnumerable<CharCollectionRow> HexDump(Stream stream,
                                                         Encoding encoding,
                                                         long offset = 0,
                                                         int length = 0,
                                                         ColorType colorType = ColorType.None)
    {
        Config config = (Config)Config.Default.Clone();
        config.Encoding = encoding;
        config.ColorType = colorType;
        return HexDump(stream, config, offset, length);
    }

    /// <inheritdoc cref="HexDump(Stream, Encoding, long, int, ColorType)"/>
    /// <param name="config">HexDump用の各種設定オブジェクト</param>
    public static IEnumerable<CharCollectionRow> HexDump(Stream stream,
                                                         Config config,
                                                         long offset = 0,
                                                         int length = 0)
    {
        long position = offset;
        var encoding = Encoding.GetEncoding(config.Encoding.CodePage, EncoderFallback.ReplacementFallback, new TopBytesFallback());
        var fallbackBuffer = ((TopBytesFallback)encoding.DecoderFallback).FallbackBuffer;
        CharCollectionRow charDatas = new(position, config);

        ConcurrentQueue<CharCollectionRow> rowQueue = [];
        AutoResetEvent rowEvent = new(false);
        bool completed = false;

        var dumpTask = AsyncHexDumpStream(stream, config.Encoding, Emit, offset, length);

        while (!(completed && rowQueue.IsEmpty))
        {
            while (rowQueue.TryDequeue(out var row))
            {
                yield return row;
            }

            if (completed)
                break;
            else
                rowEvent.WaitOne();
        }

        dumpTask.Wait();

        void Emit(long p, CharData cd)
        {
            if (p < 0)
            {
                rowQueue.Enqueue(charDatas);
                rowEvent.Set();
                completed = true;
                return;
            }
            charDatas.Set(p, cd);
            if ((p & 0x0F) == 0x0F)
            {
                rowQueue.Enqueue(charDatas);
                rowEvent.Set();
                charDatas = new(p + 1, config);
            }
        }
    }

    private const int BUFFER_LENGTH = 1024;

    private static void Seek(Stream stream, long offset)
    {
        if (offset > 0)
        {
            if (stream.CanSeek)
            {
                stream.Seek(offset, SeekOrigin.Begin);
            }
            else
            {
                long remaining = offset;
                Span<byte> skip = stackalloc byte[BUFFER_LENGTH];
                while (remaining > 0)
                {
                    int toRead = (int)Math.Min(skip.Length, remaining);
                    int read = stream.Read(skip[..toRead]);
                    if (read <= 0)
                        return;

                    remaining -= read;
                }
            }
        }
    }

    public static async Task AsyncHexDumpStream(Stream stream,
                                                Encoding originalEncoding,
                                                Action<long, CharData> emit,
                                                long offset = 0,
                                                int length = 0)
    {
        if (offset > 0)
            Seek(stream, offset);

        var encoding = Encoding.GetEncoding(originalEncoding.CodePage, EncoderFallback.ReplacementFallback, new TopBytesFallback());
        var fallbackBuffer = ((TopBytesFallback)encoding.DecoderFallback).FallbackBuffer;
        int readBytes;

        ReadOnlySpan<byte> fbBytes = ReadOnlySpan<byte>.Empty;
        long position = offset;
        int remaining = length > 0 ? length : int.MaxValue;

        Span<byte> buffer = stackalloc byte[BUFFER_LENGTH];

        do
        {
            var buf = buffer[..Math.Min(BUFFER_LENGTH, remaining)];
            fbBytes.CopyTo(buf);
            readBytes = stream.Read(buf[fbBytes.Length..]);
            if (readBytes == 0)
                break;

            HexDumpCore(buf[..(fbBytes.Length + readBytes)], encoding, fallbackBuffer, position, emit);
            if (fallbackBuffer.HasFallbackChars)
            {
                fbBytes = fallbackBuffer.GetFallbackBytes().ToArray();
            }
            remaining -= readBytes;
            position += readBytes;
        }
        while (remaining > 0);

        if (!fbBytes.IsEmpty)
        {
            position -= fbBytes.Length;
            for (var i = 0; i < fbBytes.Length; i++)
            {
                var b = fbBytes[i];
                DebugPrint($"p={position + i:X8}: (Final) {b:X2}", ConsoleColor.Yellow);
                emit(position + i, new(b, (char)b, CharType.Binary));
            }
        }

        // end signal
        emit(-1, default);
    }

    private static void HexDumpCore(ReadOnlySpan<byte> data,
                                    Encoding encoding,
                                    TopBytesFallback.TopByteFallbackBuffer fallbackBuffer,
                                    long startPosition,
                                    Action<long, CharData> emit)
    {
        const int BYTE_LENGTH = 4;
        int pos = 0;
        Span<char> charBuf = stackalloc char[8];
        Span<byte> byteBuf = stackalloc byte[BYTE_LENGTH];
        scoped Span<byte> remainingBytes = Span<byte>.Empty;
        long globalPosition;
        bool isUTF8 = encoding.CodePage == 65001;
        // fallbackBuffer = ((TopBytesFallback)encoding.DecoderFallback).FallbackBuffer;

        DebugPrint($"HexDumpCore: Start {startPosition} Lengt={data.Length}", ConsoleColor.Magenta);
        while (pos < data.Length)
        {
            int byteBufLength = Math.Min(BYTE_LENGTH, data.Length - pos);
            if (remainingBytes.IsEmpty)
            {
                data[pos..(pos + byteBufLength)].CopyTo(byteBuf);
            }
            else
            {
                remainingBytes.CopyTo(byteBuf);
                DebugPrint($"pos = {pos}, remainingBytes = {remainingBytes.Length}, byteBufLength = {byteBufLength}");
                if (pos + byteBufLength <= data.Length)
                {
                    data[(pos + remainingBytes.Length)..(pos + byteBufLength)].CopyTo(byteBuf[remainingBytes.Length..]);
                }
            }
            if (remainingBytes.Length + byteBufLength < BUFFER_LENGTH)
            {
                byteBuf = byteBuf[..byteBufLength];
            }
            DebugPrint($"TryGetChars([{string.Join(", ", byteBuf.ToArray().Select(b => $"{b:X2}"))}])", ConsoleColor.Green);
            if (!encoding.TryGetChars(byteBuf, charBuf, out int charsWritten))
            {
                throw new DecoderFallbackException($"", byteBuf.ToArray(), 0);
            }

            globalPosition = startPosition + pos;
            var byteIndex = 0;

            if (fallbackBuffer.HasFallbackChars)
            {
                DebugPrint($"HasFallbachChars: pos={pos}, remaiing={fallbackBuffer.Remaining}, dataLength={data.Length}", ConsoleColor.Yellow);
                if (pos + fallbackBuffer.Remaining >= data.Length)
                {
                    DebugPrint($"Return", ConsoleColor.Yellow);
                    return;
                }

                foreach (var fbByte in fallbackBuffer.GetFallbackBytes())
                {
                    DebugPrint($"p={pos+byteIndex:X8}: (Fallback) {fbByte:X2}", ConsoleColor.Yellow);
                    emit(globalPosition + byteIndex, new(fbByte, (char)fbByte, CharType.Binary));
                    byteIndex++;
                }
                pos += byteIndex;
                remainingBytes = byteBuf[byteIndex..];
                DebugPrint($"END: pos={pos}, remainingBytes={string.Join(' ', remainingBytes.ToArray().Select(b => $"{b:X2}"))}", ConsoleColor.Cyan);
                continue;
            }

            Rune.DecodeFromUtf16(charBuf, out Rune rune, out _);
            int byteCount = isUTF8 ? rune.Utf8SequenceLength : encoding.GetByteCount(rune.ToString());
            CharType type = byteCount > 1 ? CharType.MultiByteChar : CharType.SingleByteChar;
            CharData charData = new(byteBuf[byteIndex], rune, type);
            DebugPrint($"p={pos:X8}: [{byteCount}] {charData}", ConsoleColor.Blue);

            emit(globalPosition, charData);

            for (var j = 1; j < byteCount; j++)
            {
                type = CharType.ContinutionByte;
                if (j == 1) type |= CharType.First;
                if (j == byteCount - 1) type |= CharType.Last;
                emit(globalPosition + j, new(byteBuf[byteIndex + j], rune, type));
            }

            pos += byteCount;
            remainingBytes = byteBuf[byteCount..];
            DebugPrint($"LOOP END: pos={pos}, remainingBytes={string.Join(' ', remainingBytes.ToArray().Select(b => $"{b:X2}"))}", ConsoleColor.Cyan);
        }
    }
}
