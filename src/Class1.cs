using System.Collections;
using System.Text;

namespace MT.HexDump;

public struct CharData
{
    public byte B;
    private int Position;
    private char[] Chars;
    public bool Filled;
    public int Row => Position & 0x7FFFFFF0;
    public int Col => Position & 0x0000000F;
    public string DisplayString
    {
        get
        {
            var str = new ReadOnlySpan<char>(Chars);
            if (str.IsEmpty)
            {
                return Filled ? ".." : "  ";
            }
            if (Chars.Length == 2 && char.IsSurrogatePair(Chars[0], Chars[1]))
            {
                return str.ToString();
            }
            if (LengthInBufferCells(Chars[0]) == 2)
            {
                return str.ToString();
            }
            if (char.IsControl(str[0]))
            {
                return $"^{(char)(str[0] + 0x40)}";
            }
            return $"{str} ";
        }
    }
    public CharData(byte b, int position, char[] chars)
    {
        B = b;
        Position = position;
        Chars = chars;
        Filled = true;
    }
    public CharData(byte b, int position)
        : this(b, position, [])
    {
    }
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
    public int Count { get; } = 16;

    public bool IsEmpty = true;

    internal void Set(CharData data)
    {
        var col = data.Col;
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

    public string Row => $"0x{RowData[0].Row:X8}";
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

public static class HexDumper
{
    public static IEnumerable<CharCollectionRow> HexDump(ReadOnlyMemory<byte> data)
    {
        return HexDump(data, Encoding.UTF8);
    }
    public static IEnumerable<CharCollectionRow> HexDump(ReadOnlyMemory<byte> data, Encoding encoding)
    {
        var enc = Encoding.GetEncoding(encoding.CodePage, EncoderFallback.ReplacementFallback, DecoderFallback.ExceptionFallback);
        CharCollectionRow charDatas = new();
        foreach (var charData in HexDumpCore(data, enc))
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
    {
        int p = 0;
        while (p < data.Length)
        {
            var bytes = data.Span.Slice(p, Math.Min(4, data.Length - p));
            char[] chars = new char[2];
            int charsWritten;
            byte[]? bytesUnknown = null;
            try
            {
                enc.TryGetChars(bytes, chars, out charsWritten);
            }
            catch (DecoderFallbackException e)
            {
                bytesUnknown = e.BytesUnknown;
                charsWritten = e.Index;
            }
            if (bytes[0] == 0)
            {
                yield return new CharData(bytes[0], p++);
            }
            else if (chars[0] != default)
            {
                if (char.IsSurrogatePair(chars[0], chars[1]))
                {
                    byte[] buf = bytes.ToArray();
                    yield return new CharData(buf[0], p++, chars);
                    for (var j = 1; j < buf.Length; j++)
                    {
                        yield return new CharData(buf[j], p++);
                    }
                }
                else
                {
                    int byteCount = enc.GetByteCount(chars[..1]);
                    byte[] buf = bytes.ToArray();
                    yield return new CharData(buf[0], p++, chars[..1]);
                    for (var j = 1; j < byteCount; j++)
                    {
                        yield return new CharData(buf[j], p++);
                    }
                }
            }
            else if (bytesUnknown is not null)
            {
                for (var i = 0; i < bytesUnknown.Length; i++)
                {
                    yield return new CharData(bytesUnknown[i], p++);
                }
            }
            else
            {
                yield return new CharData(bytes[0], p++, (char[])[(char)bytes[0]]);
            }
        }
    }
}
