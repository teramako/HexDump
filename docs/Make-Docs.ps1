<#
.SYNOPSIS
Create documents for platyPS v2
#>
using namespace System.Management.Automation
using namespace System.IO

[CmdletBinding()]
param(
    [Parameter()]
    [string] $Locale = "en-US"
    ,
    [Parameter()]
    [string] $OutputFolder
)

$ErrorActionPreference = 'Stop'
$ErrorView = 'DetailedView'

Import-Module Microsoft.PowerShell.PlatyPS -Verbose:$false

$moduleName = "HexDump"
$module = Get-Module $moduleName
if ($null -eq $module)
{
    $ModulePath = Resolve-Path -RelativeBasePath $PSScriptRoot -Path ..\..\$moduleName
    Write-Verbose "Could not Get-Module $moduleName. Try Get module from $ModulePath"
    $module = Import-Module $ModulePath -Force -PassThru
}
if ([string]::IsNullOrEmpty($OutputFolder))
{
    $OutputFolder = if ($Locale) {
        Join-Path -Path $PSScriptRoot -ChildPath $Locale
    } else {
        "$PSScriptRoot"
    }
}
if (Test-Path -Path $OutputFolder)
{
    $OutputFolder = (Get-Item -Path $OutputFolder).FullName
}
else
{
    $null = New-Item -ItemType Directory -Path $OutputFolder
}
Write-Verbose "OutputFolder: $OutputFolder"

$moduleFile = Join-Path -Path $OutputFolder -ChildPath "$moduleName.md"

[CommandInfo[]] $allCommands = $module.ExportedCommands.Values.Where({ $_.CommandType -ne 'Alias' })
[FileInfo[]] $markdownFiles = Get-ChildItem -LiteralPath $OutputFolder -Filter "*-*.md"

[FileInfo[]] $newFiles = @()
[FileInfo[]] $updateFiles = $markdownFiles.Where({ $allCommands.Name -contains $_.BaseName })
[CommandInfo[]] $newCommands = $allCommands.Where({ $markdownFiles.BaseName -notcontains $_.Name })

if ($newCommands.Count -gt 0)
{
    $newFiles = New-MarkdownCommandHelp -OutputFolder $OutputFolder `
        -CommandInfo $newCommands -Locale $Locale -Encoding utf8NoBom -AbbreviateParameterTypename
}

$updatedCommandHelps = @()
if ($updateFiles.Count -gt 0)
{
    $updatedCommandHelps = $updateFiles |
        Update-MarkdownCommandHelp -NoBackup -PassThru |
        # ForEach-Object -Begin {
        #     $encoding = [System.Text.UTF8Encoding]::new($false)
        #     $total = $updateFiles.Count
        #     $index = 0
        # } -Process {
        #     $file = $_
        #     Write-Verbose ("Update file: {0}" -f $file)
        #     Write-Progress -Activity ConvertToNoBom -Status $file.Name -PercentComplete ((++$index) * 100 / $total)
        #     $contents = [System.IO.File]::ReadAllText($file)
        #     [System.IO.File]::WriteAllText($file, $contents, $encoding)
        #     $file
        # } -End {
        #     Write-Progress -Activity ConvertToNoBom -Completed
        # } |
        Import-MarkdownCommandHelp
}

if (Test-Path -Path $moduleFile)
{
    if ($updatedCommandHelps.Count -gt 0)
    {
        Update-MarkdownModuleFile -Path $moduleFile -CommandHelp $updatedCommandHelps -Locale $Locale -Encoding utf8NoBom -NoBackup -Force
    }
}
else
{
    $commands = $updatedCommandHelps
    if ($newFiles.Count -gt 0)
    {
        $commands += Import-MarkdownCommandHelp -Path $newFiles
    }
    New-MarkdownModuleFile -OutputFolder $OutputFolder `
        -CommandHelp $commands `
        -Encoding utf8NoBom `
        -Locale $Locale
}
Write-Output $newFiles
Write-Output $updateFiles

