Describe 'HexDump' {
    Context 'Cancel' {
        It 'Cancel after 1 second' {
            $job = Start-Job { Show-HexDump -Path /dev/urandom -Length 50mb }
            Start-Sleep -Second 1
            Stop-Job $job
            Receive-Job $job -Wait -AutoRemoveJob | Out-Null
            $job.State | Should -Be 'Stopped'
        }
    }
}
