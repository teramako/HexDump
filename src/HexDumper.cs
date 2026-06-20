using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace MT.HexDump;

public static partial class HexDumper
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

        void EmitBatch(long p, ReadOnlySpan<CharData> batch)
        {
            if (p < 0)
            {
                queueEvent.Set();
                completed = true;
                return;
            }
            for (int i = 0; i < batch.Length; i++)
            {
                charDataQueue.Enqueue(batch[i]);
            }
            queueEvent.Set();
        }
        void DumpTask()
        {
            var targetData = length > 0 && data.Length > offset + length
                ? data.Span.Slice((int)offset, length)
                : data.Span.Slice((int)offset);
            DebugPrint($"All bytes = [{string.Join(' ', targetData.ToArray().Select(static b => $"{b:X2}"))}]", ConsoleColor.Green);
            HexDumpCore(targetData, encoding, fallbackBuffer, offset, EmitBatch);
            if (fallbackBuffer.HasFallbackChars)
            {
                Span<CharData> batch = stackalloc CharData[4];
                var i = 0;
                foreach (var b in fallbackBuffer.GetFallbackBytes())
                {
                    batch[i++] = new(b, (char)b, CharType.Binary);
                }
                EmitBatch(offset + targetData.Length -i, batch[..i]);
            }
            // end signal
            EmitBatch(-1, default);
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

        var dumpTask = AsyncHexDumpStream(stream, config.Encoding, EmitBatch, offset, length);

        while (!(completed && rowQueue.IsEmpty))
        {
            while (rowQueue.TryDequeue(out var row))
            {
                if (row.IsEmpty)
                    yield break;
                yield return row;
            }

            if (completed)
                break;
            else
                rowEvent.WaitOne();
        }

        dumpTask.Wait();

        void EmitBatch(long p, ReadOnlySpan<CharData> batch)
        {
            if (p < 0)
            {
                rowQueue.Enqueue(charDatas);
                rowEvent.Set();
                completed = true;
                return;
            }
            for (var i = 0; i < batch.Length; i++)
            {
                var pos = p + i;
                charDatas.Set(pos, batch[i]);
                if ((pos & 0x0F) == 0x0F)
                {
                    rowQueue.Enqueue(charDatas);
                    rowEvent.Set();
                    charDatas = new(pos + 1, config);
                }
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
                                                Action<long, ReadOnlySpan<CharData>> emitBatch,
                                                long offset = 0,
                                                int length = 0)
    {
        if (offset > 0)
            Seek(stream, offset);

        long position = offset;
        int remaining = length > 0 ? length : int.MaxValue;

        Span<byte> buffer = stackalloc byte[BUFFER_LENGTH];

        switch (originalEncoding.CodePage)
        {
            case 20127: // ASCII
                ProcessingFixedByteEncoding(stream, position, remaining, buffer, emitBatch, HexDumpCoreAscii);
                return;
            case 28591: // Latin-1
                ProcessingFixedByteEncoding(stream, position, remaining, buffer, emitBatch, HexDumpCoreLatin1);
                return;
            case 65001: // UTF-8
                ProcessingUtf8(stream, position, remaining, buffer, emitBatch);
                return;
            default:
                ProcessGeneric(stream, position, remaining, buffer, emitBatch, originalEncoding);
                return;
        }
    }

    private static void ProcessingFixedByteEncoding(Stream stream,
                                                    long position,
                                                    int remaining,
                                                    Span<byte> buffer,
                                                    Action<long, ReadOnlySpan<CharData>> emitBatch,
                                                    Action<ReadOnlySpan<byte>, long, Action<long, ReadOnlySpan<CharData>>> core)
    {
        int readBytes;
        do
        {
            var buf = buffer[..Math.Min(BUFFER_LENGTH, remaining)];
            readBytes = stream.Read(buf);
            if (readBytes == 0)
                break;
            core(buf[..readBytes], position, emitBatch);
            remaining -= readBytes;
            position += readBytes;
        }
        while (remaining > 0);

        emitBatch(-1, default);
    }

    private static void ProcessingUtf8(Stream stream,
                                       long position,
                                       int remaining,
                                       Span<byte> buffer,
                                       Action<long, ReadOnlySpan<CharData>> emitBatch)
    {
        var fallbackBuffer = new TopBytesFallback.TopByteFallbackBuffer();
        ReadOnlySpan<byte> fbBytes = ReadOnlySpan<byte>.Empty;

        int readBytes;
        do
        {
            var buf = buffer[..Math.Min(BUFFER_LENGTH, remaining)];
            fbBytes.CopyTo(buf);
            readBytes = stream.Read(buf[fbBytes.Length..]);
            if (readBytes == 0)
                break;

            HexDumpCoreUTF8(buf[..readBytes], position, emitBatch, fallbackBuffer);
            if (fallbackBuffer.HasFallbackChars)
                fbBytes = fallbackBuffer.GetFallbackBytes().ToArray();

            remaining -= readBytes;
            position += readBytes;
        }
        while (remaining > 0);

        FlashFallbackBytes(fbBytes, position, emitBatch);
    }

    private static void ProcessGeneric(Stream stream,
                                       long position,
                                       int remaining,
                                       Span<byte> buffer,
                                       Action<long, ReadOnlySpan<CharData>> emitBatch,
                                       Encoding originalEncoding)
    {
        var encoding = Encoding.GetEncoding(originalEncoding.CodePage, EncoderFallback.ReplacementFallback, new TopBytesFallback());
        var fallbackBuffer = ((TopBytesFallback)encoding.DecoderFallback).FallbackBuffer;
        ReadOnlySpan<byte> fbBytes = ReadOnlySpan<byte>.Empty;

        int readBytes;
        do
        {
            var buf = buffer[..Math.Min(BUFFER_LENGTH, remaining)];
            fbBytes.CopyTo(buf);
            readBytes = stream.Read(buf[fbBytes.Length..]);
            if (readBytes == 0)
                break;

            HexDumpCore(buf[..readBytes], encoding, fallbackBuffer, position, emitBatch);
            if (fallbackBuffer.HasFallbackChars)
                fbBytes = fallbackBuffer.GetFallbackBytes().ToArray();

            remaining -= readBytes;
            position += readBytes;
        }
        while (remaining > 0);

        FlashFallbackBytes(fbBytes, position, emitBatch);
    }

    private static void FlashFallbackBytes(ReadOnlySpan<byte> fbBytes, long position, Action<long, ReadOnlySpan<CharData>> emitBatch)
    {
        if (fbBytes.IsEmpty)
        {
            emitBatch(-1, default);
            return;
        }

        Span<CharData> batch = stackalloc CharData[fbBytes.Length];
        position -= fbBytes.Length;

        for (int i = 0; i < fbBytes.Length; i++)
        {
            batch[i] = new(fbBytes[i], (char)fbBytes[i], CharType.Binary);
        }
        emitBatch(position, batch);
        emitBatch(-1, default);
    }

    private static void HexDumpCore(ReadOnlySpan<byte> data,
                                    Encoding encoding,
                                    TopBytesFallback.TopByteFallbackBuffer fallbackBuffer,
                                    long startPosition,
                                    Action<long, ReadOnlySpan<CharData>> emitBatch)
    {
        const int BYTE_LENGTH = 4;
        int pos = 0;
        Span<char> charBuf = stackalloc char[8];
        Span<byte> byteBuf = stackalloc byte[BYTE_LENGTH];
        scoped Span<byte> remainingBytes = Span<byte>.Empty;
        Span<CharData> batch = stackalloc CharData[4];
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
                    batch[byteIndex++] = new(fbByte, (char)fbByte, CharType.Binary);
                }
                emitBatch(globalPosition, batch[..byteIndex]);
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

            batch[0] = charData;

            for (var j = 1; j < byteCount; j++)
            {
                type = CharType.ContinutionByte;
                if (j == 1) type |= CharType.First;
                if (j == byteCount - 1) type |= CharType.Last;
                batch[j] = new(byteBuf[byteIndex + j], rune, type);
            }
            emitBatch(globalPosition, batch[..byteCount]);

            pos += byteCount;
            remainingBytes = byteBuf[byteCount..];
            DebugPrint($"LOOP END: pos={pos}, remainingBytes={string.Join(' ', remainingBytes.ToArray().Select(b => $"{b:X2}"))}", ConsoleColor.Cyan);
        }
    }
}
