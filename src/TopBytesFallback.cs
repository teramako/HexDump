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
        private byte[] _buffer = new byte[4];

        private int _count;

        public override int Remaining => _count;

        public override void Reset()
        {
            _count = 0;
        }

        /// <summary>
        /// Indicates whether fallback bytes are stored.
        /// </summary>
        public bool HasFallbackChars => _count > 0;

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
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index + bytesUnknown.Length, 4);
            if (index == _count)
            {
                HexDumper.DebugPrint($"Store buffer[{index}]: [{string.Join(' ', bytesUnknown.Select(static b => $"{b:X2}"))}]");
                bytesUnknown.CopyTo(_buffer, index);
                _count = index + bytesUnknown.Length;
            }
            return false;
        }

        public bool Fallback(ReadOnlySpan<byte> bytesUnknown, int index)
        {
            // `index` indicates "where the fallback began in `bytesUnknown`"
            // Since `TopBytesFallback` always retains all bytes, `index` can be ignored
            HexDumper.DebugPrint($"Store span: [{string.Join(' ', bytesUnknown.ToArray().Select(static b => $"{b:X2}"))}]");
            bytesUnknown.CopyTo(_buffer);
            _count = bytesUnknown.Length;
            return false;
        }

        /// <summary>
        /// Returns the stored fallback bytes and resets the count to 0.
        /// </summary>
        public ReadOnlySpan<byte> DrainFallbackBytes()
        {
            try
            {
                return _buffer.AsSpan(0, _count);
            }
            finally
            {
                Reset();
            }
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
