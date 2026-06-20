param(
    [Parameter()]
    [int] $Length = 1mb
)
function Get-Performance {
    param(
        [Parameter(Mandatory, Position = 0)]
        [string] $Encoding
        ,
        [Parameter()]
        [int] $Length
    )
    begin {
        Write-Host ("Show-HexDump -Path /dev/random -Length {0} -Encoding {1}" -f $Length, $Encoding) -ForegroundColor Magenta

        $GC = @{ GC0 = [GC]::CollectionCount(0); GC1=[GC]::CollectionCount(1); GC2=[GC]::CollectionCount(2); }
    }
    end {
        $time = Measure-Command {
            Show-HexDump -Path /dev/random -Length $Length -Encoding $Encoding >$null
        }
        $gcCounts = [ordered]@{ GC0 = [GC]::CollectionCount(0) - $GC.GC0; GC1=[GC]::CollectionCount(1) - $GC.GC1; GC2=[GC]::CollectionCount(2) - $GC.GC2; }

        Write-Host ("Time: {0} ms" -f $time.TotalMilliseconds) -ForegroundColor Green
        $gcCounts | Out-String | Write-Host
    }
}

Get-Performance -Encoding ASCII     -Length $Length
Get-Performance -Encoding Latin1    -Length $Length
Get-Performance -Encoding utf-8     -Length $Length
Get-Performance -Encoding shift_jis -Length $Length
Get-Performance -Encoding EUC-JP    -Length $Length
Get-Performance -Encoding utf-16    -Length $Length

