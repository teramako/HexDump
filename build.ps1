<#
.SYNOPSIS
    Build PowerShell Module Package

.PARAMETER Build
    build donet project

.PARAMETER CreateZip
    Create Zip archived the PowerShell module files

.PARAMETER Publish
    Upload the PowerShell module to PowerShell Gallery (https://www.powershellgallery.com/)

#>
[CmdletBinding()]
param(
    [Parameter(ParameterSetName = "Build", Mandatory)]
    [switch] $Build
    ,
    [Parameter(ParameterSetName = "Zip", Mandatory)]
    [switch] $CreateZip
    ,
    [Parameter(ParameterSetName = "Publish", Mandatory)]
    [switch] $Publish
    ,
    [Parameter(ParameterSetName = "HelpXML", Mandatory)]
    [switch] $HelpXML
)
$ErrorActionPreference = 'Stop'

$psmDir = "$PSScriptRoot"

$commonParam = if ($PSCmdlet.MyInvocation.BoundParameters['Verbose'])
{
    @{ Verbose = $true }
} else {
    @{ Verbose = $false }
}

$psdFile = Join-Path -Path $psmDir -ChildPath HexDump.psd1
$ModuleManifest = Test-ModuleManifest -Path $psdFile
$tmpDir = Join-Path -Path $PSScriptRoot -ChildPath out, $ModuleManifest.Name

function Build-Project
{
    param()
    '---------------------------------------------',
    'Build Project',
    '---------------------------------------------' | Write-Host -ForegroundColor Magenta
    try {
        Push-Location $psmDir
        dotnet build src -c Release | Write-Host
        if ($LASTEXITCODE -ne 0) {
            throw "Faild to build"
        }
    }
    finally {
        Pop-Location
    }
}

function CreateDest
{
    param()

    if (Test-Path -Path $tmpDir -PathType Container)
    {
        "Remove: $tmpDir" | Write-Host -ForegroundColor Magenta
        Remove-Item -Recurse $tmpDir @commonParam
    }
    $null = New-Item -Path $tmpDir -ItemType Directory

    Build-Project
    BuildMamlHelp | Write-Host
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

function BuildMamlHelp
{
    [OutputType([System.IO.FileInfo])]
    param()
    '---------------------------------------------',
    'Build Maml Help',
    '---------------------------------------------' | Write-Host -ForegroundColor Magenta
    $script = Join-Path -Path $psmDir -ChildPath 'docs', 'Make-MamlHelp.ps1'
    & $script @commonParam
}

if ($Build)
{
    Build-Project
}

if ($HelpXML)
{
    BuildMamlHelp
}

if ($CreateZip)
{
    $dir = CreateDest

    '---------------------------------------------',
    'Create Zip',
    '---------------------------------------------' | Write-Host -ForegroundColor Magenta
    $zipFileName = "{0}-{1}.zip" -f $ModuleManifest.Name, $ModuleManifest.Version.ToString()
    $zipFile = Join-Path -Path $PSScriptRoot -ChildPath out, $zipFileName
    Compress-Archive -Path $dir -DestinationPath $zipFile -PassThru -Force @commonParam
}

if ($Publish)
{
    $userName = if ($IsWindows) { $env:USERNAME } else { $env:USER }
    $nugetCredential = Get-Credential -Title "Nuget ApiKey" -UserName $userName

    $null = BuildMamlHelp
    $dir = CreateDest

    '---------------------------------------------',
    'Upload to PowerShell Gallery',
    '---------------------------------------------' | Write-Host -ForegroundColor Magenta
    Publish-Module `
        -Path $dir `
        -NuGetApiKey ($nugetCredential.Password | ConvertFrom-SecureString -AsPlainText) `
        @commonParam
}

