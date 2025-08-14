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
    )

    switch ($PSCmdlet.ParameterSetName)
    {
        'Data'
        {
            Write-Output ([HexDumper]::HexDump($Data, $Encoding))
        }
        'Stream'
        {
            throw [System.NotSupportedException]::new("Not supported currently");
        }
    }
}
