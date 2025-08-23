namespace MT.HexDump;

/// <summary>
/// ダンプ結果の配色タイプ
/// </summary>
public enum ColorType
{
    /// <summary>
    /// 色なし
    /// </summary>
    None = 0,

    /// <summary>
    /// バイト値に応じた配色
    /// </summary>
    ByByte = 1,

    /// <summary>
    /// <see cref="CharData.Type"/> に応じた配色
    /// </summary>
    ByCharType = 2,

    /// <summary>
    /// <see cref="CharData.UnicodeCategory"/> に応じた配色
    /// </summary>
    ByUnicodeCategory = 3
}
