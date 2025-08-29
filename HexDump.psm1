using namespace MT.HexDump;

function Write-HexDump
{
    [CmdletBinding()]
    [Alias("hexdump")]
    [OutputType([MT.HexDump.CharCollectionRow])]
    param(
        [Parameter(ParameterSetName="Data", Mandatory, ValueFromPipeline, Position = 0)]
        [Alias('d')]
        [byte[]] $Data
        ,
        [Parameter(ParameterSetName="Stream", Mandatory, ValueFromPipeline, Position = 0)]
        [System.IO.Stream] $Stream
        ,
        [Parameter(ParameterSetName="Path", Mandatory, ValueFromPipeline, Position = 0)]
        [string] $Path
        ,
        [Parameter()]
        [MT.HexDump.Config] $Config = [MT.HexDump.Config]::Default
        ,
        [Parameter()]
        [Alias('e')]
        [MT.HexDump.EncodingTransformation()]
        [ArgumentCompleter([MT.HexDump.EncodingArgumentCompleter])]
        [System.Text.Encoding] $Encoding
        ,
        [Parameter()]
        [ValidateRange(0, [long]::MaxValue)]
        [long] $Offset = 0
        ,
        [Parameter()]
        [ValidateRange(0, [int]::MaxValue)]
        [int] $Length = 0
        ,
        [Parameter()]
        [Alias('f')]
        [ValidateSet('SplitHexAndChars', 'UnifyHexAndChars')]
        [string] $Format
        ,
        [Parameter()]
        [Alias('c')]
        [MT.HexDump.ColorType] $Color
    )

    $newConfig = [Config] $Config.Clone();
    if ($null -ne $Encoding)
    {
        $newConfig.Encoding = $Encoding;
    }
    if ($null -ne $Color)
    {
        $newConfig.ColorType = $Color;
    }

    $dumpIter = switch ($PSCmdlet.ParameterSetName)
    {
        'Data'
        {
            [HexDumper]::HexDump($Data, $newConfig, $Offset, $Length)
        }
        'Stream'
        {
            [HexDumper]::HexDump($Stream, $newConfig, $Offset, $Length)
        }
        'Path'
        {
            $file = Get-Item -LiteralPath $Path -Force
            $Stream = $file.OpenRead()
            [HexDumper]::HexDump($Stream, $newConfig, $Offset, $Length)
        }
    }

    try
    {
        switch ($Format)
        {
            'SplitHexAndChars'
            {
                $dumpIter | Format-Table -View SplitHexAndChars
            }
            'UnifyHexAndChars'
            {
                $dumpIter | Format-Table -View UnifyHexAndChars
            }
            default
            {
                Write-Output $dumpIter
            }
        }
    }
    finally
    {
        if ($null -ne $Stream)
        {
            $Stream.Dispose()
        }
    }
}
