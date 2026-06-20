namespace MT.HexDump;

public static partial class HexDumper
{
    private static void HexDumpCoreLatin1(ReadOnlySpan<byte> data,
                                          long startPosition,
                                          Action<long, ReadOnlySpan<CharData>> emitBatch)
    {
        const int BATCH_SIZE = 512;
        int pos = 0;
        Span<CharData> batch = stackalloc CharData[BATCH_SIZE];
        while (pos < data.Length)
        {
            int batchSize = Math.Min(BATCH_SIZE, data.Length - pos);
            for (int i = 0; i < batchSize; i++)
            {
                byte b = data[pos + i];
                batch[i] = new(b, (char)b, CharType.SingleByteChar);
            }
            emitBatch(startPosition + pos, batch[..batchSize]);
            pos += batchSize;
        }
    }
}
