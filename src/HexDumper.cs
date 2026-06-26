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

        BlockingCollection<CharData> charCollection = [];

        var dumpTask = Task.Run(DumpTask);

        foreach (var charData in charCollection.GetConsumingEnumerable())
        {
            yield return charData;
        }

        dumpTask.Wait();

        void EmitBatch(long p, ReadOnlySpan<CharData> batch)
        {
            if (p < 0)
            {
                charCollection.CompleteAdding();
                return;
            }
            for (int i = 0; i < batch.Length; i++)
            {
                charCollection.Add(batch[i]);
            }
        }

        void DumpTask()
        {
            var targetData = length > 0 && data.Length > offset + length
                ? data.Span.Slice((int)offset, length)
                : data.Span.Slice((int)offset);
            DebugPrint($"All bytes = [{string.Join(' ', targetData.ToArray().Select(static b => $"{b:X2}"))}]", ConsoleColor.Green);
            try
            {
                switch (encoding.CodePage)
                {
                    case 20127: // ASCII
                        HexDumpCoreAscii(targetData, offset, EmitBatch);
                        return;
                    case 28591: // Latin-1
                        HexDumpCoreLatin1(targetData, offset, EmitBatch);
                        return;
                    case 65001: // UTF-8
                        var fallbackBuffer = new TopBytesFallback.TopByteFallbackBuffer();
                        HexDumpCoreUTF8(targetData, offset, EmitBatch, fallbackBuffer);
                        FlashFallbackBytes(fallbackBuffer.DrainFallbackBytes(), targetData.Length, EmitBatch);
                        return;
                    default:
                        var newEncoding = Encoding.GetEncoding(encoding.CodePage, EncoderFallback.ReplacementFallback, new TopBytesFallback());
                        fallbackBuffer = ((TopBytesFallback)encoding.DecoderFallback).FallbackBuffer;
                        HexDumpCore(targetData, encoding, fallbackBuffer, offset, EmitBatch);
                        FlashFallbackBytes(fallbackBuffer.DrainFallbackBytes(), targetData.Length, EmitBatch);
                        return;
                }
            }
            finally
            {
                EmitBatch(-1, default);
            }
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
                                                         ColorType colorType = ColorType.None,
                                                         CancellationToken cancellationToken = default)
    {
        Config config = (Config)Config.Default.Clone();
        config.Encoding = encoding;
        config.ColorType = colorType;
        return HexDump(stream, config, offset, length, cancellationToken);
    }

    /// <inheritdoc cref="HexDump(Stream, Encoding, long, int, ColorType)"/>
    /// <param name="config">HexDump用の各種設定オブジェクト</param>
    public static IEnumerable<CharCollectionRow> HexDump(Stream stream,
                                                         Config config,
                                                         long offset = 0,
                                                         int length = 0,
                                                         CancellationToken cancellationToken = default)
    {
        long position = offset;
        var encoding = Encoding.GetEncoding(config.Encoding.CodePage, EncoderFallback.ReplacementFallback, new TopBytesFallback());
        var fallbackBuffer = ((TopBytesFallback)encoding.DecoderFallback).FallbackBuffer;
        CharCollectionRow charDatas = new(position, config);

        BlockingCollection<CharCollectionRow> rowCollection = [];

        var dumpTask = AsyncHexDumpStream(stream, config.Encoding, EmitBatch, offset, length, cancellationToken);

        foreach (var row in rowCollection.GetConsumingEnumerable(cancellationToken))
        {
            yield return row;
        }

        dumpTask.Wait();

        void EmitBatch(long p, ReadOnlySpan<CharData> batch)
        {
            if (p < 0)
            {
                if (!charDatas.IsEmpty)
                    rowCollection.Add(charDatas);
                rowCollection.CompleteAdding();
                return;
            }
            for (var i = 0; i < batch.Length; i++)
            {
                var pos = p + i;
                charDatas.Set(pos, batch[i]);
                if ((pos & 0x0F) == 0x0F)
                {
                    rowCollection.Add(charDatas);
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
                                                int length = 0,
                                                CancellationToken cancellationToken = default)
    {
        if (offset > 0)
            Seek(stream, offset);

        long position = offset;
        int remaining = length > 0 ? length : int.MaxValue;

        try
        {
            switch (originalEncoding.CodePage)
            {
                case 20127: // ASCII
                    ProcessingFixedByteEncoding(stream,
                                                position,
                                                remaining,
                                                emitBatch,
                                                HexDumpCoreAscii,
                                                cancellationToken);
                    return;
                case 28591: // Latin-1
                    ProcessingFixedByteEncoding(stream,
                                                position,
                                                remaining,
                                                emitBatch,
                                                HexDumpCoreLatin1,
                                                cancellationToken);
                    return;
                case 65001: // UTF-8
                    ProcessingUtf8(stream,
                                   position,
                                   remaining,
                                   emitBatch,
                                   cancellationToken);
                    return;
                default:
                    ProcessGeneric(stream,
                                   position,
                                   remaining,
                                   emitBatch,
                                   originalEncoding,
                                   cancellationToken);
                    return;
            }
        }
        finally
        {
            emitBatch(-1, default);
        }
    }

    private static void ProcessingFixedByteEncoding(Stream stream,
                                                    long position,
                                                    int remaining,
                                                    Action<long, ReadOnlySpan<CharData>> emitBatch,
                                                    Action<ReadOnlySpan<byte>, long, Action<long, ReadOnlySpan<CharData>>> core,
                                                    CancellationToken cancellationToken = default)
    {
        Span<byte> buffer = stackalloc byte[BUFFER_LENGTH];
        int readBytes;
        do
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var buf = buffer[..Math.Min(BUFFER_LENGTH, remaining)];
            readBytes = stream.Read(buf);
            if (readBytes == 0)
                break;
            core(buf[..readBytes], position, emitBatch);
            remaining -= readBytes;
            position += readBytes;
        }
        while (remaining > 0);
    }

    private static void ProcessingUtf8(Stream stream,
                                       long position,
                                       int remaining,
                                       Action<long, ReadOnlySpan<CharData>> emitBatch,
                                       CancellationToken cancellationToken = default)
    {
        var fallbackBuffer = new TopBytesFallback.TopByteFallbackBuffer();
        ReadOnlySpan<byte> fbBytes = ReadOnlySpan<byte>.Empty;

        Span<byte> buffer = stackalloc byte[BUFFER_LENGTH];
        int readBytes, totalBytes;
        do
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var buf = buffer[..Math.Min(BUFFER_LENGTH, remaining + fbBytes.Length)];
            readBytes = stream.Read(buf[fbBytes.Length..]);
            if (readBytes == 0)
                break;

            fbBytes.CopyTo(buf);
            totalBytes = readBytes + fbBytes.Length;
            HexDumpCoreUTF8(buf[..totalBytes], position - fbBytes.Length, emitBatch, fallbackBuffer);
            fbBytes = fallbackBuffer.DrainFallbackBytes();

            remaining -= readBytes;
            position += readBytes;
        }
        while (remaining > 0);

        FlashFallbackBytes(fbBytes, position, emitBatch);
    }

    private static void ProcessGeneric(Stream stream,
                                       long position,
                                       int remaining,
                                       Action<long, ReadOnlySpan<CharData>> emitBatch,
                                       Encoding originalEncoding,
                                       CancellationToken cancellationToken = default)
    {
        var encoding = Encoding.GetEncoding(originalEncoding.CodePage, EncoderFallback.ReplacementFallback, new TopBytesFallback());
        var fallbackBuffer = ((TopBytesFallback)encoding.DecoderFallback).FallbackBuffer;
        ReadOnlySpan<byte> fbBytes = ReadOnlySpan<byte>.Empty;

        Span<byte> buffer = stackalloc byte[BUFFER_LENGTH];
        int readBytes, totalBytes;
        do
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var buf = buffer[..Math.Min(BUFFER_LENGTH, remaining + fbBytes.Length)];
            readBytes = stream.Read(buf[fbBytes.Length..]);
            if (readBytes == 0)
                break;

            fbBytes.CopyTo(buf);
            totalBytes = readBytes + fbBytes.Length;
            HexDumpCore(buf[..totalBytes], encoding, fallbackBuffer, position - fbBytes.Length, emitBatch);
            fbBytes = fallbackBuffer.DrainFallbackBytes();

            remaining -= readBytes;
            position += readBytes;
        }
        while (remaining > 0);

        FlashFallbackBytes(fbBytes, position, emitBatch);
    }

    private static void FlashFallbackBytes(ReadOnlySpan<byte> fbBytes, long position, Action<long, ReadOnlySpan<CharData>> emitBatch)
    {
        if (fbBytes.IsEmpty)
            return;

        Span<CharData> batch = stackalloc CharData[fbBytes.Length];
        position -= fbBytes.Length;

        for (int i = 0; i < fbBytes.Length; i++)
        {
            batch[i] = new(fbBytes[i], (char)fbBytes[i], CharType.Binary);
            DebugPrint($"p={position+i:X8}: (Fallback) {fbBytes[i]:X2}", ConsoleColor.Yellow);
        }
        emitBatch(position, batch);
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

                var fbBytes = fallbackBuffer.DrainFallbackBytes();
                for (; byteIndex < fbBytes.Length; byteIndex++)
                {
                    byte fbByte = fbBytes[byteIndex];
                    DebugPrint($"p={pos+byteIndex:X8}: (Fallback) {fbByte:X2}", ConsoleColor.Yellow);
                    batch[byteIndex] = new(fbByte, (char)fbByte, CharType.Binary);
                }
                emitBatch(globalPosition, batch[..byteIndex]);
                pos += byteIndex;
                remainingBytes = byteBuf[byteIndex..];
                DebugPrint($"END: pos={pos}, remainingBytes={string.Join(' ', remainingBytes.ToArray().Select(b => $"{b:X2}"))}", ConsoleColor.Cyan);
                continue;
            }

            Rune.DecodeFromUtf16(charBuf, out Rune rune, out _);
            int byteCount = encoding.GetByteCount(rune.ToString());
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
