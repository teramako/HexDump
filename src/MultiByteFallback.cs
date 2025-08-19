using System.Text;

namespace MT.HexDump;

/// <summary>
/// byte から char へのデコード処理時のフォールバック処理クラス。
/// <para>
/// Hexdump 処理において、別の文字へ置き換えや例外を飛ばしたくないため、無視させる。
/// </para>
/// </summary>
internal class MultiByteFallback : DecoderFallback
{
    public override int MaxCharCount => 1;

    public override DecoderFallbackBuffer CreateFallbackBuffer()
    {
        return new MultiByteFallbackBuffer();
    }
    private class MultiByteFallbackBuffer : DecoderFallbackBuffer
    {
        private byte _firstByte;
        private int _remaining;
        public override int Remaining => _remaining;

        public override bool Fallback(byte[] bytesUnknown, int index)
        {
            if (index == 0)
            {
                HexDumper.DebugPrint($"Fallback: index={index} bytesUnknown=[{string.Join(' ', bytesUnknown.Select(static b => $"{b:X2}"))}]");
                _firstByte = bytesUnknown[0];
                _remaining = 1;
                return true;
            }
            HexDumper.DebugPrint($"Ignore: index={index} bytesUnknown=[{string.Join(' ', bytesUnknown.Select(static b => $"{b:X2}"))}]");
            return false;
        }

        public override char GetNextChar()
        {
            if (_remaining == 0)
                return default;
            _remaining = 0;
            HexDumper.DebugPrint($"GetNextChar: {_firstByte}");
            return (char)_firstByte;
        }

        public override bool MovePrevious()
        {
            throw new NotImplementedException();
        }
    }
}
