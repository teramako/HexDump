namespace MT.HexDump;

public struct CharData
{
    /// <summary>
    /// データの先頭からの位置
    /// </summary>
    private long Position;
    /// <summary>
    /// 基本的には Unicode コードポイント。
    /// ただし、マイナス値の場合はマルチバイト文字における後続バイトであることを示す。
    /// </summary>
    public int CodePoint;
    /// <summary>
    /// Byte data
    /// </summary>
    public byte B;
    /// <summary>
    /// この構造体がバイトデータ、位置、コードポイントを定めた値(デフォルト値ではない)であることを示すフラグ
    /// </summary>
    public bool Filled;
    public long Row => Position & 0x7FFFFFF0;
    public long Col => Position & 0x0000000F;

    /// <summary>
    /// 標準のコンストラクタ
    /// </summary>
    /// <param name="b">そのポイントのバイトデータ</param>
    /// <param name="position">バイトデータの位置</param>
    /// <param name="codePoint">
    /// Unicodeコードポイント。マイナス値にするとマルチバイト文字における後続バイトであることになる
    /// </param>
    public CharData(byte b, long position, int codePoint)
    {
        B = b;
        Position = position;
        CodePoint = codePoint;
        Filled = true;
    }
    /// <summary>
    /// マルチバイト文字における後続バイトである場合のコンストラクタ
    /// </summary>
    /// <inheritdoc cref="CharData.CharData(byte, long, int)"/>
    public CharData(byte b, long position)
        : this(b, position, -1)
    {
    }

    private const string NULL_LETTER = "  ";
    private const string NON_LETTER = "..";
    private const string CONTINUTION_LETTER = "←─";

    /// <summary>
    /// コードポイントを単純に文字列化した値
    /// </summary>
    public string RawString => CodePoint > 0 ? char.ConvertFromUtf32(CodePoint) : string.Empty;

    /// <summary>
    /// ダンプ結果の表示用に半角2つ分の文字列を返す。
    /// </summary>
    /// <param name="showLaten1">0x80 - 0xFF の Latin1 を印字するか否か</param>
    /// <param name="cellLength">返す文字列のセル数（ターミナル上の半角文字数）</param>
    public string GetDisplayString(bool showLaten1 = false, int cellLength = 2)
    {
        var str = !Filled
            ? NULL_LETTER
            : CodePoint switch
            {
                < 0 => CONTINUTION_LETTER,
                < 0x20 => $"^{(char)(CodePoint + 0x40)}",
                < 0x7F => $"{(char)CodePoint}",
                0x7F => $"^{(char)(CodePoint - 0x40)}",
                <= 0x9F => showLaten1 ? $"^{(char)(CodePoint + 0x40)}" : NON_LETTER,
                <= 0xFF => showLaten1 ? $"{(char)CodePoint}" : NON_LETTER,
                _ => char.ConvertFromUtf32(CodePoint)
            };
        var strCellLen = LengthInBufferCells(str);
        return strCellLen < cellLength
            ? str + new string(' ', cellLength - strCellLen)
            : str;
    }

    /// <summary>
    /// 対象文字列のセル数を計算する。（ターミナル上の半角文字幾つ分になるか）
    /// </summary>
    internal static int LengthInBufferCells(string str)
    {
        return str.Sum(LengthInBufferCells);
    }

    /// <summary>
    /// 対象文字のセル数を計算する。（ターミナル上の半角文字幾つ分になるか）
    /// </summary>
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

