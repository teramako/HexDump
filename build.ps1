<#
.SYNOPSIS
Build PowerShell Module Package

.PARAMETER CreateZip
Create Zip archived the PowerShell module files

.PARAMETER Publish
Upload the PowerShell module to PowerShell Gallery (https://www.powershellgallery.com/)

#>
[CmdletBinding()]
param(
    [Parameter(ParameterSetName = "Zip", Mandatory)]
    [switch] $CreateZip
    ,
    [Parameter(ParameterSetName = "Publish", Mandatory)]
    [switch] $Publish
)
$ErrorActionPreference = 'Stop'

$psmDir = "$PSScriptRoot"

$commonParam = if ($PSCmdlet.MyInvocation.BoundParameters['Verbose'])
{
    @{ Verbose = $true }
} else {
    @{ Verbose = $false }
}

$ModuleManifest = Test-ModuleManifest -Path $psmDir\HexDump.psd1
$tmpDir = "$PSScriptRoot\out\{0}" -f $ModuleManifest.Name

function CreateDest()
{
    if (Test-Path -Path $tmpDir -PathType Container)
    {
        Remove-Item -Recurse $tmpDir @commonParam
    }
    $null = New-Item -Path $tmpDir -ItemType Directory
    $ModuleManifest.FileList | ForEach-Object {
        $filePath = Resolve-Path -Path $_ -Relative -RelativeBasePath $psmDir
        $destFile = [System.IO.FileInfo]::new((Join-Path -Path $tmpDir -ChildPath $filePath));
        $destDir = $destFile.Directory
        if (-not $destDir.Exists)
        {
            $null = New-Item -ItemType Directory -Path $destDir @commonParam
        }
        Copy-Item -Path $filePath -Destination $destDir @commonParam
    }
    return $tmpDir
}

if ($CreateZip)
{
    $dir = CreateDest
    $zipFile = "$PSScriptRoot\out\{0}-{1}.zip" -f $ModuleManifest.Name, $ModuleManifest.Version.ToString()
    Compress-Archive -Path $dir -DestinationPath $zipFile -PassThru -Force @commonParam
}

if ($Publish)
{
    $userName = if ($IsWindows) { $env:USERNAME } else { $env:USER }
    $nugetCredential = Get-Credential -Title "Nuget ApiKey" -UserName $userName

    $dir = CreateDest

    Publish-Module `
        -Path $dir `
        -NuGetApiKey ($nugetCredential.Password | ConvertFrom-SecureString -AsPlainText) `
        @commonParam
}

