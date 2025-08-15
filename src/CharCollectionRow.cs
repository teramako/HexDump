using System.Text;

namespace MT.HexDump;

public class CharCollectionRow
{
    internal CharData[] RowData = new CharData[16];
    private long _row;

    public bool IsEmpty = true;

    internal void Set(CharData data)
    {
        var col = data.Col;
        _row = data.Row;
        RowData[col] = data;
        IsEmpty = false;
    }

    public IEnumerator<CharData> GetEnumerator()
    {
        foreach(CharData c in RowData)
        {
            yield return c;
        }
    }

    public string Row => $"0x{_row:X8}";
    public string Hex
    {
        get
        {
            StringBuilder sb = new(48);
                sb.AppendJoin(' ', RowData.Select(static c => c.Filled ? $"{c.B:X2}" : "  "));
            return sb.ToString();
        }
    }
    public string Chars
    {
        get
        {
            StringBuilder sb = new(48);
            sb.AppendJoin('│', RowData.Select(static c => c.Filled ? c.DisplayString: "  "));
            return sb.ToString();
        }
    }
}

