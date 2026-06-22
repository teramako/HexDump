using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Management.Automation;
using System.Text;

namespace MT.HexDump.PowerShell;

[Cmdlet(VerbsCommon.Show, "HexDump")]
[OutputType(typeof(SplitView), typeof(UnifiedView))]
public class ShowHexDumpCommand : PSCmdlet
{
    private const string MessageBaseName = "MT.HexDump.resources.messages";
    private const string DataParameterSet = "Data";
    private const string PathParameterSet = "Path";

    [Parameter(ParameterSetName = DataParameterSet, Mandatory = true, ValueFromPipeline = true, Position = 0,
               HelpMessageBaseName = MessageBaseName, HelpMessageResourceId = "Param.Data")]
    [Alias("d")]
    public byte[] Data { get; set; } = [];

    [Parameter(ParameterSetName = PathParameterSet, Mandatory = true, Position = 0,
               HelpMessageBaseName = MessageBaseName, HelpMessageResourceId = "Param.Path")]
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

    [Parameter()]
    [Alias("v")]
    public ViewType View { get; set; }

    private Config _newConfig = Config.Default;

    private FileStream? _fs;
    private AnonymousPipeServerStream? _server;
    private AnonymousPipeClientStream? _client;
    private Task? _readerTask;

    private readonly ConcurrentQueue<CharCollectionRow> _queue = [];
    private readonly AutoResetEvent _queueEvent = new(false);
    private volatile bool _readerCompleted = false;

    private Func<CharCollectionRow, RowView> _createView = null!;

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

        _createView = View switch
        {
            ViewType.Unified => row => new UnifiedView(row),
            _ => row => new SplitView(row)
        };

        switch (ParameterSetName)
        {
            case DataParameterSet:
                _server = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None);
                _client = new AnonymousPipeClientStream(PipeDirection.In, _server.ClientSafePipeHandle);
                _readerTask = Task.Run(() => HexDumpReader(_client));
                break;
            case PathParameterSet:
                _fs = File.OpenRead(Path);
                _readerTask = Task.Run(() => HexDumpReader(_fs));
                break;
            default:
                throw new InvalidOperationException();

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
        switch (ParameterSetName)
        {
            case DataParameterSet:
                DataProcessing();
                break;
            case PathParameterSet:
                PathProcessing();
                break;
            default:
                throw new InvalidOperationException();
        }
    }

    private void FlushQueueAndWait()
    {
        while (!_readerCompleted || !_queue.IsEmpty)
        {
            while (_queue.TryDequeue(out var row))
            {
                WriteObject(_createView(row));
            }

            if (!_readerCompleted)
            {
                _queueEvent.WaitOne();
            }
        }

        _readerTask?.Wait();
    }

    private void DataProcessing()
    {
        _server?.Dispose();
        FlushQueueAndWait();
    }

    private void PathProcessing()
    {
        FlushQueueAndWait();
        _fs?.Dispose();
    }
}
