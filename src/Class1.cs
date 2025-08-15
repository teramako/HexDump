using System.Diagnostics;
using System.Text;

namespace MT.HexDump;

public struct CharData
{
    private long Position;
    public int CodePoint;
    public byte B;
    public bool Filled;
    public long Row => Position & 0x7FFFFFF0;
    public long Col => Position & 0x0000000F;
    public string DisplayString
    {
        get
        {
            if (!Filled)
                return "  ";
            switch (CodePoint)
            {
                case < 0:
                    return "..";
                case < 0x20:
                    return $"^{(char)(CodePoint + 0x40)}";
                case < 0x7F:
                    return $"{(char)CodePoint} ";
                case 0x7F:
                    return $"^{(char)(CodePoint - 0x40)}";
                case <= 0x9F:
                    return $"^{(char)(CodePoint + 0x40)}";
                case <= 0xFF:
                    return $"{(char)CodePoint} ";
                default:
                    var str = char.ConvertFromUtf32(CodePoint);
                    if (char.IsSurrogatePair(str, 0))
                    {
                        return str;
                    }
                    else if (LengthInBufferCells(str[0]) == 2)
                    {
                        return str;
                    }
                    else
                    {
                        return $"{str} ";
                    }
            }
        }
    }
    public CharData(byte b, long position, int codePoint)
    {
        B = b;
        Position = position;
        CodePoint = codePoint;
        Filled = true;
    }
    public CharData(byte b, long position)
        : this(b, position, -1)
    {
    }
    /// <seealso href="https://github.com/PowerShell/PowerShell/blob/7fe5cb3e354eb775778944e5419cfbcb8fede735/src/Microsoft.PowerShell.ConsoleHost/host/msh/ConsoleControl.cs#L2785-L2806"/>
    internal static int LengthInBufferCells(char c)
    {
        // The following is based on http://www.cl.cam.ac.uk/~mgk25/c/wcwidth.c
        // which is derived from https://www.unicode.org/Public/UCD/latest/ucd/EastAsianWidth.txt
        bool isWide = c >= 0x1100 &&
            (c <= 0x115f || /* Hangul Jamo init. consonants */
             c == 0x2329 || c == 0x232a ||
             ((uint)(c - 0x2e80) <= (0xa4cf - 0x2e80) &&
              c != 0x303f) || /* CJK ... Yi */
             ((uint)(c - 0xac00) <= (0xd7a3 - 0xac00)) || /* Hangul Syllables */
             ((uint)(c - 0xf900) <= (0xfaff - 0xf900)) || /* CJK Compatibility Ideographs */
             ((uint)(c - 0xfe10) <= (0xfe19 - 0xfe10)) || /* Vertical forms */
             ((uint)(c - 0xfe30) <= (0xfe6f - 0xfe30)) || /* CJK Compatibility Forms */
             ((uint)(c - 0xff00) <= (0xff60 - 0xff00)) || /* Fullwidth Forms */
             ((uint)(c - 0xffe0) <= (0xffe6 - 0xffe0)));

        // We can ignore these ranges because .Net strings use surrogate pairs
        // for this range and we do not handle surrogate pairs.
        // (c >= 0x20000 && c <= 0x2fffd) ||
        // (c >= 0x30000 && c <= 0x3fffd)
        return 1 + (isWide ? 1 : 0);
    }
}

public class CharCollectionRow
{
    internal CharData[] RowData = new CharData[16];
    private long _row;

    public bool IsEmpty = true;

    internal void Set(CharData data)
    {
        var col = data.Col;
        _row = data.Row;
        RowData[col] = data;
        IsEmpty = false;
    }

    public IEnumerator<CharData> GetEnumerator()
    {
        foreach(CharData c in RowData)
        {
            yield return c;
        }
    }

    public string Row => $"0x{_row:X8}";
    public string Hex
    {
        get
        {
            StringBuilder sb = new(48);
                sb.AppendJoin(' ', RowData.Select(static c => c.Filled ? $"{c.B:X2}" : "  "));
            return sb.ToString();
        }
    }
    public string Chars
    {
        get
        {
            StringBuilder sb = new(48);
            sb.AppendJoin('│', RowData.Select(static c => c.Filled ? c.DisplayString: "  "));
            return sb.ToString();
        }
    }
}

/// <summary>
/// byte から char へのデコード処理時のフォールバック処理クラス。
/// <para>
/// Hexdump 処理において、別の文字へ置き換えや例外を飛ばしたくないため、無視させる。
/// </para>
/// </summary>
internal class IgnoreFallback : DecoderFallback
{
    public override int MaxCharCount => 1;

    public override DecoderFallbackBuffer CreateFallbackBuffer()
    {
        return new IgnoreFallbackBufer();
    }
    private class IgnoreFallbackBufer : DecoderFallbackBuffer
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

public static class HexDumper
{
    [Conditional("DEBUG")]
    public static void DebugPrint(string msg, ConsoleColor foreground = ConsoleColor.Red)
    {
        var c = Console.ForegroundColor;
        Console.ForegroundColor = foreground;
        Console.Error.WriteLine(msg);
        Console.ForegroundColor = c;
    }

    public static IEnumerable<CharCollectionRow> HexDump(ReadOnlyMemory<byte> data, long offset = 0)
    {
        return HexDump(data, Encoding.UTF8, offset);
    }
    public static IEnumerable<CharCollectionRow> HexDump(ReadOnlyMemory<byte> data, Encoding encoding, long offset = 0)
    {
        if (data.Length < offset)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset value too large for data length {data.Length}.");
        }
        if (offset > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset must be smaller than int ({int.MaxValue})");
        }
        var enc = Encoding.GetEncoding(encoding.CodePage, EncoderFallback.ReplacementFallback, new IgnoreFallback());
        CharCollectionRow charDatas = new();
        foreach (var charData in HexDumpCore(data.Slice((int)offset), enc, offset))
        {
            var col = charData.Col;
            charDatas.Set(charData);
            if (col == 0x0F)
            {
                yield return charDatas;
                charDatas = new();
            }
        }
        if (!charDatas.IsEmpty)
        {
            yield return charDatas;
        }
    }

    private static IEnumerable<CharData> HexDumpCore(ReadOnlyMemory<byte> data, Encoding enc)
    private static IEnumerable<CharData> HexDumpCore(ReadOnlyMemory<byte> data, Encoding enc, long offset = 0)
    {
        int p = 0;
        while (p < data.Length)
        {
            var bytes = data.Span.Slice(p, Math.Min(4, data.Length - p)).ToArray();
            char[] chars = new char[2];
            enc.TryGetChars(bytes, chars, out _);
            if (chars[0] <= 0xFF)
            {
                DebugPrint($"p={p:X8}: char[0]=0x{(int)chars[0]:X2} [0x00 - 0xFF] bytes=[{string.Join(' ', bytes.Select(static b => $"{b:X2}"))}]", ConsoleColor.Blue);
                yield return new CharData(bytes[0], offset + p++, (int)chars[0]);
            }
            else if (char.IsSurrogatePair(chars[0], chars[1]))
            {
                DebugPrint($"p={p:X8}: char=[0x{(int)chars[0]:X2}, 0x{(int)chars[1]:X2}] [SurrogatePair] bytes=[{string.Join(' ', bytes.Select(static b => $"{b:X2}"))}]", ConsoleColor.Blue);
                yield return new CharData(bytes[0], offset + p++, char.ConvertToUtf32(chars[0], chars[1]));
                for (var j = 1; j < bytes.Length; j++)
                {
                    yield return new CharData(bytes[j], offset + p++);
                }
            }
            else
            {
                int byteCount = enc.GetByteCount(chars[..1]);
                DebugPrint($"p={p:X8}: char[0]=0x{(int)chars[0]:X2} [> 0xFF] bytes=[{string.Join(' ', bytes.Select(static b => $"{b:X2}"))}]", ConsoleColor.Blue);
                yield return new CharData(bytes[0], offset + p++, (int)chars[0]);
                for (var j = 1; j < byteCount; j++)
                {
                    yield return new CharData(bytes[j], offset + p++);
                }
            }
        }
    }
}
