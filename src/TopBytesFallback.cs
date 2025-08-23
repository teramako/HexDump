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
        private byte[] _bytesUnknown = [];
        /// <returns>常に <c>0</c> を返し、<see cref="Decoder"/> に処理させない</returns>
        /// <inheritdoc cref="DecoderFallbackBuffer.Remaining"/>
        public override int Remaining => 0;

        public override void Reset()
        {
            _bytesUnknown = [];
        }

        /// <summary>
        /// フォールバック用の値が入っているか否か。
        /// </summary>
        public bool HasFallbackChars => _bytesUnknown.Length > 0;

        /// <summary>
        /// 後々の処理としてフォールバックした文字
        /// (<see cref="CharData.IsChar" /> が <c>false</c> なデータ)
        /// を返す。
        /// </summary>
        public IEnumerable<CharData> GetFallbackChars()
        {
            foreach (var b in _bytesUnknown)
            {
                yield return new CharData(b, (char)b, CharType.Binary);
            }
            Reset();
        }

        /// <summary>
        /// <paramref name="index"/> が <c>0</c> の時のみ、後で処理できるように貯めておく。
        /// </summary>
        /// <inheritdoc cref="DecoderFallbackBuffer.Fallback(byte[], int)"/>
        public override bool Fallback(byte[] bytesUnknown, int index)
        {
            if (index == 0)
            {
                HexDumper.DebugPrint($"Store buffer: [{string.Join(' ', bytesUnknown.Select(static b => $"{b:X2}"))}]");
                _bytesUnknown = bytesUnknown;
                return true;
            }
            return false;
        }

        /// <summary>
        /// <see cref="Decoder"/> がフォールバック文字を受け取るために呼ばれる関数。
        /// ここで本来フォールバック文字を返すと、フォールバックした文字なのか
        /// デコードに成功した文字なのか区別が付かない。
        /// <see cref="Decoder"/> には何もさせないように、<c>NUL 0x00</c> を返す。
        /// </summary>
        /// <returns>常に <c>NUL 0x00</c> を返す</returns>
        public override char GetNextChar()
        {
            return default;
        }

        public override bool MovePrevious()
        {
            throw new NotImplementedException();
        }
    }
}
