using System.Text;

namespace MT.HexDump;

/// <summary>
/// <see cref="CharData"/> 16個を入れた行
/// </summary>
public class CharCollectionRow(long row, Config config)
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
    /// 各種設定値
    /// </summary>
    public Config Config { get; set; } = config;

    public string Hex => GetHexRow();

    /// <remarks>
    /// 配色設定はこのインスタンスに設定された <see cref="ColorType"/> が用いられる
    /// </remarks>
    /// <inheritdoc cref="GetHexRow(Config, int)"/>
    public string GetHexRow(int cellLength = 3)
    {
        return GetHexRow(Config, cellLength);
    }
    /// <summary>
    /// バイトデータの16進数値行を返す。
    /// </summary>
    /// <inheritdoc cref="PrintHexRow(StringBuilder, Config, int)"/>
    public string GetHexRow(Config config, int cellLength)
    {
        StringBuilder sb = new(RowData.Length * (cellLength + config.HexColumnSeparator.Length));
        PrintHexRow(sb, config, cellLength);
        return sb.ToString();
    }

    /// <summary>
    /// 各バイトデータの16進数値を <paramref name="sb"/> に書き込む
    /// </summary>
    /// <param name="sb">値を追加する <see cref="StringBuilder"/> インタンス</param>
    /// <param name="config">各種設定値</param>
    /// <param name="cellLength">1データのセル数</param>
    private void PrintHexRow(StringBuilder sb, Config config, int cellLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cellLength, 2, nameof(cellLength));
        var remainingCellCount = cellLength - 2;
        for (var i = 0; i < RowData.Length; i++)
        {
            CharData c = RowData[i];
            if (i != 0)
            {
                if (config.ColorType is not ColorType.None)
                    sb.Append(Color.Reset);
                sb.Append(config.HexColumnSeparator);
            }
            if (c.Filled)
            {
                c.PrintColor(sb, config);
                sb.Append($"{c.B:X2}")
                  .Append(' ', remainingCellCount);
            }
            else
            {
                sb.Append(' ', cellLength);
            }
        }
    }

    public string Chars => GetCharsRow();

    /// <remarks>
    /// 配色設定はこのインスタンスに設定された <see cref="ColorType"/> が用いられる
    /// </remarks>
    /// <inheritdoc cref="GetCharsRow(Config, int)"/>
    public string GetCharsRow(int cellLength = 2)
    {
        return GetCharsRow(Config, cellLength);
    }
    /// <summary>
    /// 表示文字列用の行を返す。
    /// </summary>
    /// <inheritdoc cref="PrintCharsRow(StringBuilder, Config, int)"/>
    public string GetCharsRow(Config config, int cellLength)
    {
        StringBuilder sb = new(RowData.Length * (cellLength + config.CharColumnSeparator.Length));
        PrintCharsRow(sb, config, cellLength);
        return sb.ToString();
    }

    /// <summary>
    /// 各バイトデータの文字列化した値を <paramref name="sb"/> に追加する
    /// </summary>
    /// <param name="sb">値を追加する <see cref="StringBuilder"/> インタンス</param>
    /// <param name="cellLength">1データのセル数</param>
    private void PrintCharsRow(StringBuilder sb, Config config, int cellLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cellLength, 2, nameof(cellLength));
        for (var i = 0; i < RowData.Length; i++)
        {
            CharData c = RowData[i];
            if (i != 0)
            {
                if (config.ColorType is not ColorType.None)
                    sb.Append(Color.Reset);
                sb.Append(config.CharColumnSeparator);
            }
            if (c.Filled)
            {
                c.PrintColor(sb, config);
                c.PrintDisplayString(sb, config, cellLength);
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
    /// <inheritdoc cref="GetHexAndCharsRow(Config, int)"/>
    public string GetHexAndCharsRow(int cellLength = 3)
    {
        return GetHexAndCharsRow(Config, cellLength);
    }
    /// <summary>
    /// 各 <see cref="RowData"/> のHex行とChars行を改行で区切った2行にして返す。
    /// </summary>
    /// <param name="cellLength">1データのセル数</param>
    public string GetHexAndCharsRow(Config config, int cellLength)
    {
        (int hexSepLen, int charSepLen) = (config.HexColumnSeparator.Length, config.CharColumnSeparator.Length);
        int maxSepLen = Math.Max(hexSepLen, charSepLen);
        int hexCellLen = (maxSepLen + cellLength) - hexSepLen;
        int charCellLen = (maxSepLen + cellLength) - charSepLen;

        StringBuilder sb = new(RowData.Length * (cellLength + maxSepLen) * 2);
        PrintHexRow(sb, config, hexCellLen);
        sb.AppendLine();
        PrintCharsRow(sb, config, charCellLen);
        return sb.ToString();
    }

    public int Count => IsEmpty ? 0 : RowData.Count(static c => c.Filled);
}

