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
    public static string GetFromHLS(int h, int l, int s)
    {
        return RGB.FromHLS(h, l, s).ToTermBg();
    }

    /// <summary>
    /// バイト値に対応するターミナルのエスケープシーケンスを返す。
    /// </summary>
    /// <param name="b">バイト値。HLSカラーの色相 (Hue)にマッピングされる。0 - 255</param>
    /// <param name="config">Config</param>
    public static string GetColorFromByte(byte b, Config? config = null)
    {
        config ??= Config.Default;
        return GetFromHLS(config.InitialHue + (int)(b * 360.0 / 0xFF),
                          config.DefaultLightness,
                          config.DefaultSaturation);
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
    /// <param name="config">Config</param>
    public static string GetFromCharType(CharType charType, int codePoint, Config? config = null)
    {
        config ??= Config.Default;
        return charType switch
        {
            CharType.Binary /* data which failed decode to a char */
                => GetFromHLS(config.InitialHue,
                              config.DefaultLightness,
                              0), // Gray scale
            CharType.SingleByteChar => codePoint switch
            {
                < 0x20 or 0x7F /* ASCII Control chars */
                    => GetFromHLS(config.InitialHue,
                                  config.DefaultLightness,
                                  config.DefaultSaturation), 
                < 0x7F /* ASCII chars */
                    => GetFromHLS(config.InitialHue + 90,
                                  config.DefaultLightness,
                                  config.DefaultSaturation),
                < 0xA0 /* Non-ASCII control chars */
                    => GetFromHLS(config.InitialHue + 10,
                                  config.DefaultLightness,
                                  config.DefaultSaturation),
                _ /* Non-ASCII chars */
                    => GetFromHLS(config.InitialHue + 120,
                                  config.DefaultLightness,
                                  config.DefaultSaturation),
            },
            CharType.MultiByteChar
                or CharType.ContinutionByte
                or CharType.ContinutionFirstByte
                or CharType.ContinutionFirstAndLastByte
                or CharType.ContinutionLastByte
                => GetFromHLS(config.InitialHue + 150,
                              config.DefaultLightness,
                              config.DefaultSaturation),
            _ => string.Empty
        };
    }

    /// <summary>
    /// <see cref="UnicodeCategory"/> 値から対応するターミナルエスケープシーケンスを返す。
    /// <para>
    /// <see cref="UnicodeCategory"/> 値は 0 - 29 の30個ある。
    /// </para>
    /// </summary>
    /// <param name="uc">Unicode Category</param>
    /// <param name="config">Config</param>
    public static string GetFromUnicodeCategory(UnicodeCategory? uc, Config? config = null)
    {
        config ??= Config.Default;
        const double NumberOfCategories = 30.0;
        return uc is null
            ? string.Empty
            : GetFromHLS(config.InitialHue + (int)Math.Ceiling((int)uc / NumberOfCategories * 360.0),
                         config.DefaultLightness,
                         config.DefaultSaturation);
    }
}
