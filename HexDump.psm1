using namespace MT.HexDump;

function Write-HexDump
{
    [CmdletBinding()]
    [Alias("hexdump")]
    [OutputType([MT.HexDump.CharCollectionRow])]
    param(
        [Parameter(ParameterSetName="Data", Mandatory, ValueFromPipeline)]
        [byte[]] $Data
        ,
        [Parameter(ParameterSetName="Stream", Mandatory, ValueFromPipeline)]
        [System.IO.Stream] $Stream
        ,
        [Parameter()]
        [System.Text.Encoding] $Encoding = [System.Text.Encoding]::UTF8
        ,
        [Parameter()]
        [ValidateRange(0, [long]::MaxValue)]
        [long] $Offset = 0
        ,
        [Parameter()]
        [ValidateRange(0, [int]::MaxValue)]
        [int] $Length = 0
    )

    switch ($PSCmdlet.ParameterSetName)
    {
        'Data'
        {
            Write-Output ([HexDumper]::HexDump($Data, $Encoding, $Offset, $Length))
        }
        'Stream'
        {
            Write-Output ([HexDumper]::HexDump($Stream, $Encoding, $Offset, $Length))
        }
    }
}
