using System.Text;

namespace MT.HexDump;

/// <summary>
/// HexDump用のフォールバック処理クラス。
/// 先頭バイト列のみフォールバックできるようにする。
/// </summary>
/// <remarks>
/// <para>
/// Hexdump 処理において、デコード対象のバイト列は文字列とは限らず高確率で失敗するだろう。
/// 本来デコードに成功するだろうバイト列であっても、
/// (特にストリーム処理では全てのバイト列をデコード対象にできず)
/// 途中で途切れたバイト列であるために変換に失敗するかもしれない。
/// よって、先頭バイトのみをバッファーに貯めておく。
/// </para>
/// <para>
/// また、本来のフォールバックでは代替文字を返すか例外を投げるのだが、ここでは何もしない。
/// 後々処理できるようにする。
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
    /// HexDump用のフォールバック・バッファー。
    /// </summary>
    /// <inheritdoc cref="TopBytesFallback"/>
    internal class TopByteFallbackBuffer : DecoderFallbackBuffer
    {
        private Queue<byte> _buffer = new();
        /// <returns>常に <c>0</c> を返し、<see cref="Decoder"/> に処理させない</returns>
        /// <inheritdoc cref="DecoderFallbackBuffer.Remaining"/>
        public override int Remaining => 0;

        public override void Reset()
        {
            _buffer.Clear();
        }

        /// <summary>
        /// フォールバック用の値が入っているか否か。
        /// </summary>
        public bool HasFallbackChars => _buffer.Count > 0;

        /// <summary>
        /// 後々の処理としてフォールバックした文字
        /// (<see cref="CharData.IsChar" /> が <c>false</c> なデータ)
        /// を返す。
        /// </summary>
        public IEnumerable<CharData> GetFallbackChars()
        {
            while (_buffer.TryDequeue(out byte b))
            {
                yield return new CharData(b, (char)b, CharType.Binary);
            }
            Reset();
        }

        /// <summary>
        /// <paramref name="index"/> が <c>0</c> から連続している場合のみ、後で処理できるように貯めておく。
        /// </summary>
        /// <remarks>
        /// 戻り値は常に <c>false</c> とし、<see cref="Decoder"/> にフォールバック処理をさせない。
        /// </remarks>
        /// <inheritdoc cref="DecoderFallbackBuffer.Fallback(byte[], int)"/>
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

        public override char GetNextChar()
        {
            throw new NotImplementedException();
        }

        public override bool MovePrevious()
        {
            throw new NotImplementedException();
        }
    }
}
