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
        private ByteRingBuffer _buffer = new();

        /// <inheritdoc/>
        public override int Remaining => _buffer.Count;

        public override void Reset()
        {
            _buffer.Clear();
        }

        /// <summary>
        /// Indicates whether fallback bytes are stored.
        /// </summary>
        public bool HasFallbackChars => _buffer.HasData;

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

        public bool Fallback(ReadOnlySpan<byte> bytesUnknown, int index)
        {
            // `index` indicates "where the fallback began in `bytesUnknown`"
            // Since `TopBytesFallback` always retains all bytes, `index` can be ignored
            for (var i = 0; i < bytesUnknown.Length; i++)
            {
                _buffer.Enqueue(bytesUnknown[i]);
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

    internal sealed class ByteRingBuffer
    {
        private readonly byte[] _buffer = new byte[4];
        private int _head = 0;
        private int _tail = 0;
        private int _count = 0;

        public int Count => _count;
        public bool HasData => _count > 0;

        public void Clear()
        {
            _head = _tail = _count = 0;
        }

        public void Enqueue(byte b)
        {
            if (_count == 4)
                throw new InvalidOperationException("RingBuffer overflow");

            _buffer[_tail] = b;
            _tail = (_tail + 1) & 3; // %4 の高速化
            _count++;
        }

        public bool TryDequeue(out byte b)
        {
            if (_count == 0)
            {
                b = default;
                return false;
            }
            b = _buffer[_head];
            _head = (_head + 1) & 3;
            _count--;
            return true;
        }

        public IEnumerable<byte> Drain()
        {
            while (TryDequeue(out var b))
            {
                yield return b;
            }
        }
    }
}
