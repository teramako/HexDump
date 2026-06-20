<#
.SYNOPSIS
    Generate Maml Help
#>
param(
    [Parameter()]
    [cultureinfo] $Locale = "en-US"
    ,
    [Parameter()]
    [switch] $Force
)

$moduleName = "HexDump"

$ErrorActionPreference = 'Stop'
$ErrorView = 'DetailedView'

Import-Module Microsoft.PowerShell.PlatyPS -Verbose:$false

$ProjectDir = Resolve-Path -RelativeBasePath $PSScriptRoot -Path ..
$moduleHelpRoot = Join-Path -Path $PSScriptRoot -ChildPath $moduleName
$moduleFile = Join-Path -Path $moduleHelpRoot -ChildPath $Locale, "README.md"

$moduleInfo = Import-MarkdownModuleFile -Path $moduleFile

$commandInfoFiles = $moduleInfo.CommandGroups.Where({ $_.GroupTitle -eq $moduleName }).Commands.Name |
    ForEach-Object {
        Join-Path -Path $moduleHelpRoot -ChildPath $Locale, "$_.md"
    }

$outputFolder = Join-Path -Path $ProjectDir -ChildPath out, $moduleName, $Locale
if (-not (Test-Path $outputFolder)) {
    $null = New-Item -Type Directory -Path $outputFolder
}
$commandInfoFiles |
    ForEach-Object {
        Write-Verbose "Import-MarkdownCommandHelp: $_"
        Import-MarkdownCommandHelp -LiteralPath $_
    } |
        Export-MamlCommandHelp -Encoding utf8NoBOM `
                               -OutputFolder $PSScriptRoot `
                               -Verbose |
         Move-Item -Destination $outputFolder -Force -PassThru -Verbose

