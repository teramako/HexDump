using System.Text;

namespace MT.HexDump;

/// <summary>
/// Fallback handler used for HexDump processing.
/// Prevents the <see cref="Decoder"/> from performing its default fallback
/// and allows manual handling of undecodable byte sequences.
/// </summary>
/// <remarks>
/// <para>
/// In hex dump processing, the input byte sequence is not necessarily valid text,
/// and decoding is likely to fail. Even byte sequences that would normally decode
/// correctly may fail during streaming because the data may be incomplete.
/// Therefore, only the leading undecodable bytes are stored in the buffer.
/// </para>
/// <para>
/// A standard fallback would either return a replacement character or throw an
/// exception, but this implementation intentionally performs no fallback action.
/// The undecodable bytes are preserved so they can be processed later.
/// </para>
/// </remarks>
internal class TopBytesFallback : DecoderFallback
{
    public override int MaxCharCount => 1;

    private readonly Lazy<TopByteFallbackBuffer> _fallbackBuffer = new(()=> new());
    public TopByteFallbackBuffer FallbackBuffer => _fallbackBuffer.Value;

    public override DecoderFallbackBuffer CreateFallbackBuffer()
    {
        return _fallbackBuffer.Value;
    }
    /// <summary>
    /// Fallback buffer for HexDump.
    /// </summary>
    /// <inheritdoc cref="TopBytesFallback"/>
    internal class TopByteFallbackBuffer : DecoderFallbackBuffer
    {
        private Queue<byte> _buffer = new();

        /// <inheritdoc/>
        public override int Remaining => _buffer.Count;

        public override void Reset()
        {
            _buffer.Clear();
        }

        /// <summary>
        /// Indicates whether fallback bytes are stored.
        /// </summary>
        public bool HasFallbackChars => _buffer.Count > 0;

        /// <summary>
        /// Returns the stored fallback bytes.
        /// </summary>
        public IEnumerable<byte> GetFallbackBytes()
        {
            while (_buffer.TryDequeue(out byte b))
            {
                yield return b;
            }
            Reset();
        }

        /// <summary>
        /// Stores the fallback bytes only when <paramref name="index"/> is a
        /// continuous sequence starting from <c>0</c>, so they can be processed later.
        /// </summary>
        /// <remarks>
        /// Always returns <c>false</c> to prevent the <see cref="Decoder"/> from
        /// performing its default fallback behavior.
        /// </remarks>
        /// <inheritdoc/>
        public override bool Fallback(byte[] bytesUnknown, int index)
        {
            if (index == _buffer.Count)
            {
                HexDumper.DebugPrint($"Store buffer[{index}]: [{string.Join(' ', bytesUnknown.Select(static b => $"{b:X2}"))}]");
                foreach (byte b in bytesUnknown)
                {
                    _buffer.Enqueue(b);
                }
            }
            return false;
        }

        /// <remarks>
        /// Not implemented because fallback processing is intentionally disabled.
        /// </remarks>
        /// <inheritdoc/>
        public override char GetNextChar()
        {
            throw new NotImplementedException();
        }

        /// <remarks>
        /// Not implemented because fallback processing is intentionally disabled.
        /// </remarks>
        /// <inheritdoc/>
        public override bool MovePrevious()
        {
            throw new NotImplementedException();
        }
    }
}
