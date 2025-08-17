BeforeAll {
    $script:asciiData = [byte[]]@(0x00..0xFF)
}

Describe 'HexDump' {
    Context 'Data' {
        It 'ASCII' {
            $result = Write-HexDump -Data $script:asciiData -Encoding ASCII

            $result | Out-String | Write-Host -ForegroundColor DarkGray
        }
    }
}
