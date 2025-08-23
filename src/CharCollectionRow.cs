using System.Text;

namespace MT.HexDump;

/// <summary>
/// <see cref="CharData"/> 16個を入れた行
/// </summary>
public class CharCollectionRow(long row, ColorType colorType = ColorType.None)
{
    internal CharData[] RowData = new CharData[16];
    private long _row = row & 0x7FFFFFF0;

    /// <summary>
    /// <see cref="CharData"/> が一つでもセットされたかのフラグ
    /// </summary>
    internal bool IsEmpty = true;

    internal void Set(long position, CharData data)
    {
        RowData[position & 0x0F] = data;
        IsEmpty = false;
    }

    public IEnumerator<CharData> GetEnumerator()
    {
        return RowData.Where(static c => c.Filled).GetEnumerator();
    }

    public string Row => $"0x{_row:X8}";

    /// <summary>
    /// ダンプ結果の色付けに使用するカラータイプ
    /// </summary>
    public ColorType ColorType { get; set; } = colorType;

    public string Hex => GetHexRow();

    /// <remarks>
    /// 配色設定はこのインスタンスに設定された <see cref="ColorType"/> が用いられる
    /// </remarks>
    /// <inheritdoc cref="GetHexRow(ColorType, string, int)"/>
    public string GetHexRow(string separator = " ", int cellLength = 2)
    {
        return GetHexRow(ColorType, separator, cellLength);
    }
    /// <summary>
    /// バイトデータの16進数値行を返す。
    /// </summary>
    /// <inheritdoc cref="PrintHexRow(StringBuilder, ColorType, string, int)"/>
    public string GetHexRow(ColorType colorType, string separator = " ", int cellLength = 2)
    {
        StringBuilder sb = new(RowData.Length * (cellLength + separator.Length));
        PrintHexRow(sb, colorType, separator, cellLength);
        return sb.ToString();
    }

    /// <summary>
    /// 各バイトデータの16進数値を <paramref name="sb"/> に書き込む
    /// </summary>
    /// <param name="sb">値を追加する <see cref="StringBuilder"/> インタンス</param>
    /// <param name="colorType">配色設定</param>
    /// <param name="separator">区切り文字列</param>
    /// <param name="cellLength">1データのセル数</param>
    private void PrintHexRow(StringBuilder sb, ColorType colorType, string separator = " ", int cellLength = 2)
    {
        var remainingCellCount = cellLength - 2;
        for (var i = 0; i < RowData.Length; i++)
        {
            CharData c = RowData[i];
            if (i != 0)
            {
                sb.Append(separator);
                if (colorType is not ColorType.None)
                    sb.Append(Color.Reset);
            }
            if (c.Filled)
            {
                c.PrintColor(sb, colorType);
                sb.Append($"{c.B:X2}")
                  .Append(' ', remainingCellCount);
            }
            else
            {
                sb.Append(' ', cellLength);
            }
        }
    }

    public string Chars => GetCharsRow(" ");

    /// <remarks>
    /// 配色設定はこのインスタンスに設定された <see cref="ColorType"/> が用いられる
    /// </remarks>
    /// <inheritdoc cref="GetCharsRow(ColorType, string, int)"/>
    public string GetCharsRow(string separator = " ", int cellLength = 2)
    {
        return GetCharsRow(ColorType, separator, cellLength);
    }
    /// <summary>
    /// 表示文字列用の行を返す。
    /// </summary>
    /// <inheritdoc cref="PrintCharsRow(StringBuilder, string, int, bool)"/>
    public string GetCharsRow(ColorType colorType, string separator = " ", int cellLength = 2)
    {
        StringBuilder sb = new(RowData.Length * (cellLength + separator.Length));
        PrintCharsRow(sb, colorType, separator, cellLength);
        return sb.ToString();
    }

    /// <summary>
    /// 各バイトデータの文字列化した値を <paramref name="sb"/> に追加する
    /// </summary>
    /// <param name="sb">値を追加する <see cref="StringBuilder"/> インタンス</param>
    /// <param name="colorType">配色設定</param>
    /// <param name="separator">区切り文字列</param>
    /// <param name="cellLength">1データのセル数</param>
    private void PrintCharsRow(StringBuilder sb, ColorType colorType, string separator = " ", int cellLength = 2)
    {
        for (var i = 0; i < RowData.Length; i++)
        {
            CharData c = RowData[i];
            if (i != 0)
            {
                sb.Append(separator);
                if (colorType is not ColorType.None)
                    sb.Append(Color.Reset);
            }
            if (c.Filled)
            {
                c.PrintColor(sb, colorType);
                c.PrintDisplayString(sb, cellLength);
            }
            else
            {
                sb.Append(' ', cellLength);
            }
        }
    }

    /// <remarks>
    /// 配色設定はこのインスタンスに設定された <see cref="ColorType"/> が用いられる
    /// </remarks>
    /// <inheritdoc cref="GetHexAndCharsRow(ColorType, string, int)"/>
    public string GetHexAndCharsRow(string separator = " ", int cellLength = 2)
    {
        return GetHexAndCharsRow(ColorType, separator, cellLength);
    }
    /// <summary>
    /// 各 <see cref="RowData"/> のHex行とChars行を改行で区切った2行にして返す。
    /// </summary>
    /// <param name="colorType">配色設定</param>
    /// <param name="separator">区切り文字列</param>
    /// <param name="cellLength">1データのセル数</param>
    public string GetHexAndCharsRow(ColorType colorType, string separator = " ", int cellLength = 2)
    {
        StringBuilder sb = new(RowData.Length * (cellLength + separator.Length) * 2);
        PrintHexRow(sb, colorType, separator, cellLength);
        sb.AppendLine();
        PrintCharsRow(sb, colorType, separator, cellLength);
        return sb.ToString();
    }

    public int Count => IsEmpty ? 0 : RowData.Count(static c => c.Filled);
}

