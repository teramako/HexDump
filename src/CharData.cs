using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace MT.HexDump;

/// <summary>
/// バイト値の文字データを表す。
/// マルチバイト文字においては、Unicodeコードポイント(UTF-32)で文字を示す。
/// （文字として表すのは先頭バイト値のみ）
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct CharData
{
    /// <summary>
    /// Unicode コードポイント
    /// </summary>
    public int CodePoint;
    /// <summary>
    /// Byte data
    /// </summary>
    public byte B;

    /// <summary>
    /// バイト値の種類を表す。
    /// <list type="table">
    ///     <listheader><term>Name</term><description>Description</description></listheader>
    ///     <item>
    ///         <term><see cref="CharType.Empty"/></term>
    ///         <description>未割当。文字としてレンダリングされない。</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="CharType.Binary"/></term>
    ///         <description>デコードできなかったバイト値。文字としてレンダリングされない。</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="CharType.SingleByteChar"/></term>
    ///         <description>1バイト文字</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="CharType.MultiByteChar"/></term>
    ///         <description>マルチバイトバイト文字</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="CharType.ContinutionByte"/></term>
    ///         <description>マルチバイト文字の後続バイト。文字としてレンダリングされない。</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="CharType.ContinutionFirstByte"/></term>
    ///         <description>マルチバイト文字の後続バイトの最初。文字としてレンダリングされない。</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="CharType.ContinutionFirstAndLastByte"/></term>
    ///         <description>マルチバイト文字の後続バイトの最初と最後（2バイト文字の2番目を示すことになる）。文字としてレンダリングされない。</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="CharType.ContinutionLastByte"/></term>
    ///         <description>マルチバイト文字の後続バイトの最後。文字としてレンダリングされない。</description>
    ///     </item>
    /// </list>
    /// </summary>
    public readonly CharType Type;

    /// <summary>
    /// この構造体がバイトデータ、位置、コードポイントを定めた値(デフォルト値ではない)であることを示すフラグ
    /// </summary>
    internal bool Filled => Type is not CharType.Empty;

    /// <summary>
    /// 文字データであるか否か。
    /// （マルチバイト文字の後続バイト値のデータは除外される）
    /// </summary>
    public bool IsChar => Type.HasFlag(CharType.Char);

    /// <summary>
    /// 標準のコンストラクタ
    /// </summary>
    /// <param name="b">そのポイントのバイトデータ</param>
    /// <param name="codePoint">Unicodeコードポイント。</param>
    /// <param name="type">バイト値の文字種</param>
    public CharData(byte b, int codePoint, CharType type)
    {
        B = b;
        CodePoint = codePoint;
        Type = type;
    }

    private const string NULL_LETTER = "  ";
    private const string NON_LETTER = "..";
    private const string CONTINUTION_LETTER_FIRST = "←─";
    private const string CONTINUTION_LETTER = "──";

    /// <summary>
    /// コードポイントを単純に文字列化した値
    /// </summary>
    public string RawString => Filled ? char.ConvertFromUtf32(CodePoint) : string.Empty;

    public UnicodeCategory? UnicodeCategory
    {
        get
        {
            var str = RawString;
            if (string.IsNullOrEmpty(str))
                return null;
            return char.GetUnicodeCategory(str, 0);
        }
    }

    /// <summary>
    /// ダンプ結果の表示用の文字列を返す
    /// </summary>
    public string GetDisplayString()
    {
        return Type switch
        {
            CharType.Empty => NULL_LETTER,
            CharType.Binary => NON_LETTER,
            CharType.SingleByteChar => CodePoint switch
            {
                < 0x20 => $"^{(char)(CodePoint + 0x40)}",
                < 0x7F => $"{(char)CodePoint}",
                0x7F => $"^{(char)(CodePoint - 0x40)}",
                < 0xA0 => $"^{(char)(CodePoint + 0x40)}",
                _ => char.ConvertFromUtf32(CodePoint)
            },
            CharType.MultiByteChar => char.ConvertFromUtf32(CodePoint),
            CharType.ContinutionFirstByte or CharType.ContinutionFirstAndLastByte
                => CONTINUTION_LETTER_FIRST,
            _ => CONTINUTION_LETTER
        };
    }

    /// <summary>
    /// ダンプ結果の表示用の文字列を <paramref name="sb"/> へ書き込む
    /// </summary>
    /// <param name="sb">値を追加する <see cref="StringBuilder"/> インタンス</param>
    /// <param name="cellLength">セル数。足りない場合は末尾に半角空白が埋められる</param>
    internal void PrintDisplayString(StringBuilder sb, int cellLength = 2)
    {
        var str = GetDisplayString();
        var strCellLen = LengthInBufferCells(str);
        sb.Append(str);
        if (strCellLen < cellLength)
        {
            sb.Append(Type switch
            {
                CharType.Empty => NULL_LETTER[1],
                CharType.Binary => NON_LETTER[1],
                CharType.SingleByteChar or CharType.MultiByteChar => ' ',
                _ => str[1]
            }, cellLength - strCellLen);
        }
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

    public override string ToString()
    {
        return !Filled
            ? "<Empty>"
            : IsChar
              ? $"Byte: 0x{B:X2} CodePoint: U+{CodePoint:X8} <{UnicodeCategory}> {GetDisplayString}"
              : $"Byte: 0x{B:X2}";
    }
}

