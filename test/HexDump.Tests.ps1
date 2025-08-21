BeforeAll {
    $script:asciiData = [byte[]]@(0x00..0xFF)
}

Describe 'HexDump' {
    Context 'Data' {
        It 'ASCII' {
            $result = Write-HexDump -Data $script:asciiData -Encoding ASCII

            $result | Out-String | Write-Host -ForegroundColor DarkGray
        }

        It 'Latin1' {
            $result = Write-HexDump -Data $script:asciiData -Encoding Latin1

            $result | Out-String | Write-Host -ForegroundColor DarkGray
        }

        It 'UTF-8' {
            $data = [System.Text.Encoding]::UTF8.GetBytes('あいうえお')

            $result = Write-HexDump -Data $data -Encoding UTF8

            $result | Out-String | Write-Host -ForegroundColor DarkGray
        }

        It 'Shift_JIS' {
            $data = [System.Text.Encoding]::GetEncoding('shift_jis').GetBytes('あいうえお')

            $result = Write-HexDump -Data $data -Encoding Shift_JIS

            $result | Out-String | Write-Host -ForegroundColor DarkGray
        }
    }
}
