using System.Text;

namespace MT.HexDump;

internal class SingleByteFallback : DecoderFallback
{
    public override int MaxCharCount => 1;

    public override DecoderFallbackBuffer CreateFallbackBuffer()
    {
        return new SingileByteFallbackBufer();
    }
    private class SingileByteFallbackBufer : DecoderFallbackBuffer
    {
        private byte[] _bytesUnknown = [];
        private int _remaining;
        public override int Remaining => _remaining;

        public override bool Fallback(byte[] bytesUnknown, int index)
        {
            HexDumper.DebugPrint($"Fallback: index={index} bytesUnknown=[{string.Join(' ', bytesUnknown.Select(static b => $"{b:X2}"))}]");
            _bytesUnknown = bytesUnknown;
            _remaining = bytesUnknown.Length;
            return true;
        }

        public override char GetNextChar()
        {
            if (_remaining == 0)
                return default;
            var i = _bytesUnknown.Length - _remaining--;
            HexDumper.DebugPrint($"GetNextChar: {_bytesUnknown[i]}");
            return (char)_bytesUnknown[i];
        }

        public override bool MovePrevious()
        {
            HexDumper.DebugPrint($"MovePrevious: remaining: {_remaining}");
            if (_remaining == _bytesUnknown.Length)
            {
                return false;
            }
            _remaining++;
            return true;
        }
    }
}
