namespace MT.HexDump.PowerShell;

public abstract class RowView(CharCollectionRow row)
{
    protected readonly CharCollectionRow _data = row;

    public IEnumerable<CharData> ListChars => _data.RowData.Where(static c => c.Filled);
}

public sealed class SplitView(CharCollectionRow row) : RowView(row)
{
    public string Row => _data.Row;
    public string Hex => _data.GetHexRow();
    public string Chars => _data.GetCharsRow();
}

public sealed class UnifiedView(CharCollectionRow row) : RowView(row)
{
    public string Row => _data.Row;
    public string HexAndChars => _data.GetHexAndCharsRow();
}
