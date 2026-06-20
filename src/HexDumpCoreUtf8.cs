using System.Buffers;
using System.Text;

namespace MT.HexDump;

public static partial class HexDumper
{
    private static void HexDumpCoreUTF8(ReadOnlySpan<byte> data,
                                        long startPosition,
                                        Action<long, ReadOnlySpan<CharData>> emitBatch,
                                        TopBytesFallback.TopByteFallbackBuffer fallbackBuffer)
    {
        const int BATCH_SIZE = 512;
        int pos = 0;
        Span<CharData> batch = stackalloc CharData[BATCH_SIZE];
        int batchCount = 0;
        long position = startPosition;

        while (pos < data.Length)
        {
            var status = Rune.DecodeFromUtf8(data[pos..], out Rune rune, out int bytesConsumed);

            if (status is OperationStatus.NeedMoreData)
            {
                fallbackBuffer.Fallback(data[pos..], 0);
                emitBatch(position, batch[..batchCount]);
                return;
            }

            if (status is OperationStatus.InvalidData)
            {
                if (batchCount == BATCH_SIZE)
                {
                    emitBatch(position, batch);
                    position += batchCount;
                    batchCount = 0;
                }
                batch[batchCount++] = new(data[pos], (char)data[pos], CharType.Binary);
                pos++;
                continue;
            }

            if (batchCount + bytesConsumed > BATCH_SIZE)
            {
                emitBatch(position, batch[..batchCount]);
                position += batchCount;
                batchCount = 0;
            }

            batch[batchCount++] = new(data[pos], rune, bytesConsumed > 1 ? CharType.MultiByteChar : CharType.SingleByteChar);

            for (int j = 1; j < bytesConsumed; j++)
            {
                CharType type = CharType.ContinutionByte;
                if (j == 1) type |= CharType.First;
                if (j == bytesConsumed - 1) type |= CharType.Last;
                batch[batchCount++] = new(data[pos + j], rune, type);
            }

            pos += bytesConsumed;
        }

        if (batchCount > 0)
            emitBatch(position, batch[..batchCount]);
    }
}
