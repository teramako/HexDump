<#
.SYNOPSIS
    Create/Update documents with platyPS
#>
param(
    [Parameter()]
    [cultureinfo] $Locale = "en-US"
    ,
    [Parameter()]
    [switch] $Update
)

$moduleName = "HexDump"

$ErrorActionPreference = 'Stop'
$ErrorView = 'DetailedView'

$ProjectDir = Resolve-Path -RelativeBasePath $PSScriptRoot -Path ..
Import-Module $ProjectDir
Import-Module Microsoft.PowerShell.PlatyPS -Verbose:$false

$module = Get-Module -Name $moduleName

$Cmdlets = $module.ExportedCmdlets.Values

$OutputDir = Join-Path -Path $PSScriptRoot -ChildPath $moduleName, $Locale
if (-not (Test-Path -Path $OutputDir)) {
    $null = New-Item -ItemType Directory -Path $OutputDir -Verbose
}

$resultFiles = @()
foreach ($cmdlet in $Cmdlets) {
    $mdFile = Join-Path -Path $OutputDir -ChildPath ('{0}.md' -f $cmdlet.Name)

    $resultFiles += $mdFile

    if (Test-Path -Path $mdFile) {
        if ($Update) {
            Update-MarkdownCommandHelp -Path $mdFile -NoBackup -Verbose
        } else {
            Get-Item -Path $mdFile
        }
    } else {
        $newFile = New-MarkdownCommandHelp -OutputFolder $PSScriptRoot -Locale $Locale -Encoding utf8NoBOM -CommandInfo $cmdlet
        Move-Item -Path $newFile -Destination $OutputDir -Verbose -PassThru
    }
}

if ($resultFiles.Count -gt 0) {
    $destFile = Join-Path -Path $OutputDir -ChildPath 'README.md'
    $cmdHelps = Import-MarkdownCommandHelp -Path $resultFiles
    if (Test-Path $destFile) {
        Update-MarkdownModuleFile -Path $destFile -CommandHelp $cmdHelps -Locale $Locale -Force -NoBackup -Encoding utf8NoBOM
    } else {
        $indexFile = New-MarkdownModuleFile -CommandHelp $cmdHelps -OutputFolder $PSScriptRoot -Locale $Locale -Encoding utf8NoBOM
        Move-Item -Path $indexFile -Destination $destFile -Force -Verbose -PassThru
    }
}

