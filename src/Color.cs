using System.Globalization;

namespace MT.HexDump;

/// <summary>
/// Helper functions calculate Terminal Escape Sequences for color value
/// </summary>
public static class Color
{
    public const string Reset = "\u001b[0m";

    /// <summary>
    /// HLS カラーのターミナルのエスケープシーケンスを返す。
    /// </summary>
    /// <inheritdoc cref="RGB.FromHLS(int, int, int)"/>
    public static string GetFromHLS(int h, int l = 40, int s = 60)
    {
        return RGB.FromHLS(h, l, s).ToTermBg();
    }

    /// <summary>
    /// バイト値に対応するターミナルのエスケープシーケンスを返す。
    /// </summary>
    /// <param name="b">バイト値。HLSカラーの色相 (Hue)にマッピングされる。0 - 255</param>
    /// <param name="l">輝度 (Lightness) 0 - 100</param>
    /// <param name="s">彩度 (Saturation) 0 - 100</param>
    public static string GetColorFromByte(byte b, int l = 40, int s = 60)
    {
        return GetFromHLS((int)(b * 360.0 / 0xFF), l, s);
    }

    /// <summary>
    /// <see cref="CharData.Type"/> および <see cref="CharData.CodePoint"/> に対応するターミナルエスケープシーケンスを返す。
    /// <para>
    /// 以下に分類して返す。
    /// <list type="bullet">
    ///     <item><term>Binary</term><description>デコードできなかった文字</description></item>
    ///     <item><term>SingleByteChar</term><description>1バイト文字。さらに制御文字か、ASCII外の文字かで色分けする</description></item>
    ///     <item><term>MultiByteChar</term><description>マルチバイト文字</description></item>
    ///     <item><term>その他</term><description>基本的に無色</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="charType"></param>
    /// <param name="codePoint"></param>
    public static string GetFromCharType(CharType charType, int codePoint)
    {
        return charType switch
        {
            CharType.Binary => GetFromHLS(0, s: 0), // Gray scale
            CharType.SingleByteChar => codePoint switch
            {
                < 0x20 or 0x7F => GetFromHLS(120),
                < 0x7F => GetFromHLS(210),
                < 0xA0 => GetFromHLS(90),
                _ => GetFromHLS(270),
            },
            CharType.MultiByteChar
                or CharType.ContinutionByte
                or CharType.ContinutionFirstByte
                or CharType.ContinutionFirstAndLastByte
                or CharType.ContinutionLastByte
                => GetFromHLS(240),
            _ => string.Empty
        };
    }

    /// <summary>
    /// <see cref="UnicodeCategory"/> 値から対応するターミナルエスケープシーケンスを返す。
    /// <para>
    /// <see cref="UnicodeCategory"/> 値は 0 - 28 の29個ある。
    /// </para>
    /// </summary>
    /// <param name="uc">Unicode Category</param>
    public static string GetFromUnicodeCategory(UnicodeCategory? uc)
    {
        const double NumberOfCategories = 29.0;
        return uc is null
            ? string.Empty
            : GetFromHLS((int)Math.Ceiling((int)uc / NumberOfCategories * 360.0));
    }
}
