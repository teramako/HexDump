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
        [Parameter()]
        [Alias('e')]
        [MT.HexDump.EncodingTransformation()]
        [ArgumentCompleter([MT.HexDump.EncodingArgumentCompleter])]
        [System.Text.Encoding] $Encoding = [System.Text.Encoding]::UTF8
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
        [Alias('c')]
        [Parameter()]
        [MT.HexDump.ColorType] $Color = [MT.HexDump.ColorType]::None
    )

    $dumpIter = switch ($PSCmdlet.ParameterSetName)
    {
        'Data'
        {
            [HexDumper]::HexDump($Data, $Encoding, $Offset, $Length, $Color)
        }
        'Stream'
        {
            [HexDumper]::HexDump($Stream, $Encoding, $Offset, $Length, $Color)
        }
    }

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
