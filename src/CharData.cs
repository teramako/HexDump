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
public struct CharData(byte b, Rune rune, CharType type)
{
    /// <summary>
    /// Unicode ルーン文字
    /// </summary>
    public readonly Rune Rune = rune;

    /// <summary>
    /// Byte data
    /// </summary>
    public readonly byte B = b;

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
    public readonly CharType Type = type;

    /// <summary>
    /// Unicode Code Point
    /// </summary>
    public int CodePoint => Rune.Value;

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
    /// Unicodeコードポイント指定のコンストラクタ
    /// </summary>
    /// <param name="b">そのポイントのバイトデータ</param>
    /// <param name="codePoint">Unicodeコードポイント。</param>
    /// <param name="type">バイト値の文字種</param>
    public CharData(byte b, int codePoint, CharType type)
        : this(b, new Rune(codePoint), type)
    { }

    /// <summary>
    /// コードポイントを単純に文字列化した値
    /// </summary>
    public string RawString => Filled ? Rune.ToString()  : string.Empty;

    public UnicodeCategory? UnicodeCategory => Filled ? Rune.GetUnicodeCategory(Rune) : null;

    /// <summary>
    /// ダンプ結果の表示用の文字列を返す
    /// </summary>
    public string DisplayString => IsChar
        ? Type switch
        {
            CharType.SingleByteChar => Rune.Value switch
            {
                < 0x20 => $"^{(char)(Rune.Value + 0x40)}",
                < 0x7F => $"{Rune}",
                0x7F => $"^{(char)(Rune.Value - 0x40)}",
                < 0xA0 => $"^{(char)(Rune.Value + 0x40)}",
                _ => Rune.ToString()
            },
            CharType.MultiByteChar => Rune.ToString(),
            _ => string.Empty
        }
        : string.Empty;

    /// <summary>
    /// ダンプ結果の表示用の文字列を <paramref name="sb"/> へ書き込む
    /// </summary>
    /// <param name="sb">値を追加する <see cref="StringBuilder"/> インタンス</param>
    /// <param name="config">各種設定値</param>
    /// <param name="cellLength">セル数。足りない場合は末尾に半角空白が埋められる</param>
    internal void PrintDisplayString(StringBuilder sb, Config config, int cellLength)
    {
        var str = Type switch
        {
            CharType.Empty => new string(config.NullLeter, cellLength),
            CharType.Binary => new string(config.NonLetter, cellLength),
            CharType.SingleByteChar => Rune.Value switch
            {
                < 0x20 => $"{config.AsciiControlLetters[Rune.Value]}",
                < 0x7F => $"{Rune}",
                0x7F => $"{config.AsciiControlLetters[0x21]}",
                < 0xA0 => $"^{(char)(Rune.Value + 0x40)}",
                _ => Rune.ToString()
            },
            CharType.MultiByteChar => Rune.ToString(),
            CharType.ContinutionFirstByte => Rune.ToString(),
            CharType.ContinutionFirstAndLastByte => Rune.ToString(),
            CharType.ContinutionLastByte => Rune.ToString(),
            CharType.ContinutionByte => Rune.ToString(),
            _ => new string(config.NullLeter, cellLength)
        };
        var strCellLen = LengthInBufferCells(str);
        switch (Type)
        {
            case CharType.MultiByteChar when cellLength > strCellLen:
                sb.Append($"{str}{config.ContinutionLetters[0]}")
                  .Append(config.ContinutionLetters[1], cellLength - strCellLen - 1);
                break;
            case CharType.ContinutionFirstByte:
                if (cellLength > strCellLen)
                {
                    sb.Append(config.ContinutionLetters[1], cellLength);
                }
                else
                {
                    sb.Append(config.ContinutionLetters[0])
                      .Append(config.ContinutionLetters[1], cellLength - 1);
                }
                break;
            case CharType.ContinutionFirstAndLastByte:
                if (cellLength > strCellLen)
                {
                    sb.Append(config.ContinutionLetters[1], cellLength - 1)
                      .Append(config.ContinutionLetters[2]);
                }
                else
                {
                    sb.Append(config.ContinutionLetters[0])
                      .Append(config.ContinutionLetters[1], cellLength - 2)
                      .Append(config.ContinutionLetters[2]);
                }
                break;
            case CharType.ContinutionLastByte:
                sb.Append(config.ContinutionLetters[1], cellLength -1)
                  .Append(config.ContinutionLetters[2]);
                break;
            case CharType.ContinutionByte:
                sb.Append(config.ContinutionLetters[1], cellLength);
                break;
            default:
                sb.Append(str);
                if (strCellLen < cellLength)
                {
                    sb.Append(Type switch
                    {
                        CharType.SingleByteChar => ' ',
                        _ => str[^1]
                    }, cellLength - strCellLen);
                }
                break;
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

    /// <summary>
    /// <paramref name="colorType"/> に応じた色(エスケープシーケンス)を <paramref name="sb"/> へ書き込む
    /// </summary>
    /// <param name="sb">StringBuilder</param>
    /// <param name="config">各種設定値</param>
    internal void PrintColor(StringBuilder sb, Config config)
    {
        var escapeSequence = config.ColorType switch
        {
            ColorType.ByByte => Color.GetColorFromByte(B),
            ColorType.ByCharType => Color.GetFromCharType(Type, CodePoint),
            ColorType.ByUnicodeCategory => Color.GetFromUnicodeCategory(UnicodeCategory),
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(escapeSequence))
            return;

        sb.Append(escapeSequence);
    }

    public override string ToString()
    {
        return !Filled
            ? "<Empty>"
            : IsChar
              ? $"Byte: 0x{B:X2} CodePoint: U+{Rune.Value:X8} <{UnicodeCategory}> {DisplayString}"
              : $"Byte: 0x{B:X2}";
    }
}

