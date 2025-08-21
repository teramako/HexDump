using System.Text;

namespace MT.HexDump;

/// <summary>
/// <see cref="CharData"/> 16個を入れた行
/// </summary>
public class CharCollectionRow(long row)
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
    public string Hex => GetHexRow();

    /// <summary>
    /// バイトデータの16進数値行を返す。
    /// </summary>
    /// <inheritdoc cref="PrintHexRow(StringBuilder, string, int)"/>
    public string GetHexRow(string separator = " ", int cellLength = 2)
    {
        StringBuilder sb = new(RowData.Length * (cellLength + separator.Length));
        PrintHexRow(sb, separator, cellLength);
        return sb.ToString();
    }

    /// <summary>
    /// 各バイトデータの16進数値を <paramref name="sb"/> に書き込む
    /// </summary>
    /// <param name="sb">値を追加する <see cref="StringBuilder"/> インタンス</param>
    /// <param name="separator">区切り文字列</param>
    /// <param name="cellLength">1データのセル数</param>
    public void PrintHexRow(StringBuilder sb, string separator = " ", int cellLength = 2)
    {
        var remainingCellCount = cellLength - 2;
        for (var i = 0; i < RowData.Length; i++)
        {
            if (i != 0)
            {
                sb.Append(separator);
            }
            if (RowData[i].Filled)
            {
                sb.Append($"{RowData[i].B:X2}")
                  .Append(' ', remainingCellCount);
            }
            else
            {
                sb.Append(' ', cellLength);
            }
        }
    }

    public string Chars => GetCharsRow(" ");

    /// <summary>
    /// 表示文字列用の行を返す。
    /// </summary>
    /// <inheritdoc cref="PrintCharsRow(StringBuilder, string, int, bool)"/>
    public string GetCharsRow(string separator = " ", int cellLength = 2)
    {
        StringBuilder sb = new(RowData.Length * (cellLength + separator.Length));
        PrintCharsRow(sb, separator, cellLength);
        return sb.ToString();
    }

    /// <summary>
    /// 各バイトデータの文字列化した値を <paramref name="sb"/> に追加する
    /// </summary>
    /// <param name="sb">値を追加する <see cref="StringBuilder"/> インタンス</param>
    /// <param name="separator">区切り文字列</param>
    /// <param name="cellLength">1データのセル数</param>
    public void PrintCharsRow(StringBuilder sb, string separator = " ", int cellLength = 2)
    {
        for (var i = 0; i < RowData.Length; i++)
        {
            if (i != 0)
            {
                sb.Append(separator);
            }
            RowData[i].PrintDisplayString(sb, cellLength);
        }
    }

    public int Count => IsEmpty ? 0 : RowData.Count(static c => c.Filled);
}

