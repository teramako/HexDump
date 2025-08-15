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

