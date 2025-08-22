using namespace MT.HexDump;

function Write-HexDump
{
    [CmdletBinding()]
    [Alias("hexdump")]
    [OutputType([MT.HexDump.CharCollectionRow])]
    param(
        [Parameter(ParameterSetName="Data", Mandatory, ValueFromPipeline, Position = 0)]
        [byte[]] $Data
        ,
        [Parameter(ParameterSetName="Stream", Mandatory, ValueFromPipeline, Position = 0)]
        [System.IO.Stream] $Stream
        ,
        [Parameter()]
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
        [ValidateSet('SplitHexAndChars', 'UnifyHexAndChars')]
        [string] $Format
        ,
        [Parameter()]
        [switch] $Color
    )

    $dumpIter = switch ($PSCmdlet.ParameterSetName)
    {
        'Data'
        {
            [HexDumper]::HexDump($Data, $Encoding, $Offset, $Length)
        }
        'Stream'
        {
            [HexDumper]::HexDump($Stream, $Encoding, $Offset, $Length)
        }
    }

    switch ($Format)
    {
        'SplitHexAndChars'
        {
            $dumpIter.SetColoring($Color) | Format-Table -View SplitHexAndChars
        }
        'UnifyHexAndChars'
        {
            $dumpIter.SetColoring($Color) | Format-Table -View UnifyHexAndChars
        }
        default
        {
            Write-Output $dumpIter.SetColoring($Color)
        }
    }
}
