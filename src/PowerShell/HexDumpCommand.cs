using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Management.Automation;
using System.Text;

namespace MT.HexDump.PowerShell;

[Cmdlet(VerbsCommon.Show, "HexDump")]
[OutputType(typeof(CharCollectionRow))]
public class ShowHexDumpCommand : PSCmdlet
{
    private const string DataParameterSet = "Data";
    private const string PathParameterSet = "Path";

    [Parameter(ParameterSetName = DataParameterSet, Mandatory = true, ValueFromPipeline = true, Position = 0)]
    [Alias("d")]
    public byte[] Data { get; set; } = [];

    [Parameter(ParameterSetName = PathParameterSet, Mandatory = true, Position = 0)]
    public string Path { get; set; } = string.Empty;

    [Parameter()]
    public Config? Config { get; set; }

    [Parameter()]
    [Alias("e")]
    [EncodingTransformation]
    [ArgumentCompleter(typeof(EncodingArgumentCompleter))]
    public Encoding? Encoding { get; set; }

    [Parameter()]
    [ValidateRange(0, long.MaxValue)]
    public long Offset { get; set; }

    [Parameter()]
    [ValidateRange(0, int.MaxValue)]
    public int Length { get; set; }

    [Parameter()]
    [Alias("c")]
    public ColorType Color { get; set; } = ColorType.None;

    private Config _newConfig = Config.Default;

    private AnonymousPipeServerStream? _server;
    private AnonymousPipeClientStream? _client;
    private Task? _readerTask;

    private readonly ConcurrentQueue<CharCollectionRow> _queue = [];
    private readonly AutoResetEvent _queueEvent = new(false);
    private volatile bool _readerCompleted = false;

    protected override void BeginProcessing()
    {
        _newConfig = (Config)(Config?.Clone() ?? Config.Default.Clone());

        if (Encoding is not null)
        {
            _newConfig.Encoding = Encoding;
        }

        if (Color is not ColorType.None)
        {
            _newConfig.ColorType = Color;
        }

        if (ParameterSetName is DataParameterSet)
        {
            _server = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None);
            _client = new AnonymousPipeClientStream(PipeDirection.In, _server.ClientSafePipeHandle);
            _readerTask = Task.Run(() => HexDumpReader(_client));
        }
    }

    private void HexDumpReader(Stream stream)
    {
        foreach (var row in HexDumper.HexDump(stream, _newConfig, Offset, Length))
        {
            _queue.Enqueue(row);
            _queueEvent.Set();
        }

        _readerCompleted = true;
        _queueEvent.Set();
    }

    protected override void ProcessRecord()
    {
        if (ParameterSetName is DataParameterSet && _server is not null)
        {
            _server.Write(Data, 0, Data.Length);
        }
    }

    protected override void EndProcessing()
    {
        if (ParameterSetName is DataParameterSet)
        {
            _server?.Dispose();
            _readerTask?.Wait();

            while (!_readerCompleted || !_queue.IsEmpty)
            {
                while (_queue.TryDequeue(out var row))
                {
                    WriteObject(row);
                }

                if (!_readerCompleted)
                {
                    _queueEvent.WaitOne();
                }
            }
            return;
        }

        if (ParameterSetName is PathParameterSet)
        {
            using var fs = File.OpenRead(Path);
            foreach (var row in HexDumper.HexDump(fs, _newConfig, Offset, Length))
            {
                WriteObject(row);
            }
            return;
        }
    }
}
