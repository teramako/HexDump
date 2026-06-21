Describe 'HexDump' {
    BeforeAll {
        function BytesToString([byte[]] $data) {
            return $data.ForEach({ '{0:X2}' -f $_ }) -join ' '
        }
        $Script:file = Join-Path -Path $PSScriptRoot -ChildPath 'assets', '1026bytes.bin'
        $fs = [System.IO.File]::OpenRead($Script:file);
        $fs.Seek(1008, 0);
        $bytes = [byte[]]::new(18);
        $fs.read($bytes, 0, $bytes.Length);
        $fs.Close()

        $Script:actual = BytesToString $bytes
    }

    Context 'Fallback' {
        It 'handles fallback at buffer boundary: <Encoding>' -ForEach @(
            @{ Encoding = 'ascii' }
            @{ Encoding = 'latin1' }
            @{ Encoding = 'utf-8' }
            @{ Encoding = 'euc-jp' }
        ) {
            $resultBytes = (Show-HexDump -Path $Script:file -Encoding $Encoding).ListChars.B | Select-Object -Last 18
            BytesToString $resultBytes | Should -BeExactly $Script:actual
        }

        It 'handles single-byte incomplete sequence: <Encoding>' -ForEach @(
            @{ Encoding = 'ascii' }
            @{ Encoding = 'latin1' }
            @{ Encoding = 'utf-8' }
            @{ Encoding = 'euc-jp' }
        ) {
            $data = 'F0'
            $bytes = $data.Split().ForEach({ [byte]::Parse($_, [System.Globalization.NumberStyles]::HexNumber) })
            $resultBytes = (Show-HexDump -Data $bytes -Encoding $Encoding).ListChars.B
            BytesToString $resultBytes | Should -BeExactly $data
        }

        It 'handles fallback twice: <Encoding>' -ForEach @(
            @{ Encoding = 'euc-jp' }
            @{ Encoding = 'utf-8' }
        ) {
            $bytes = [byte[]]::new(1023) + @(0xF4,0x80)
            $actual = BytesToString ($bytes | Select-Object -Last 17)

            $resultBytes = (Show-HexDump -Data $bytes -Encoding $Encoding).ListChars.B | Select-Object -Last 17
            BytesToString $resultBytes | Should -BeExactly $actual
        }
    }
}

