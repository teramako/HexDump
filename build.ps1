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

function BuildMamlHelp()
{
    $cmdHelps = Get-ChildItem -Recurse -Path "$psmDir/docs" -Include "*-*.md" | Import-MarkdownCommandHelp
    $cmdHelps | Group-Object { $_.Metadata['Locale'] } | ForEach-Object {
        $name = $_.Name;
        $modules = $_.Group.ModuleName | Sort-Object -Unique
        $dir = switch ($name) {
            'en-US' { $psmDir }
            default { Join-Path -Path $psmDir -ChildPath $name }
        }
        $_.Group | Export-MamlCommandHelp -OutputFolder $dir @commonParam | ForEach-Object {
            Move-Item -Path $_ -Destination $dir -Force @commonParam
        }
        $modules | ForEach-Object {
            $moduleDir = Join-Path -Path $dir -ChildPath $_
            if (Test-Path -Path $moduleDir)
            {
                Remove-Item -Recurse -Path $moduleDir @commonParam
            }
        }
    }
}

if ($CreateZip)
{
    BuildMamlHelp
    $dir = CreateDest
    $zipFile = "$PSScriptRoot\out\{0}-{1}.zip" -f $ModuleManifest.Name, $ModuleManifest.Version.ToString()
    Compress-Archive -Path $dir -DestinationPath $zipFile -PassThru -Force @commonParam
}

if ($Publish)
{
    $userName = if ($IsWindows) { $env:USERNAME } else { $env:USER }
    $nugetCredential = Get-Credential -Title "Nuget ApiKey" -UserName $userName

    BuildMamlHelp
    $dir = CreateDest

    Publish-Module `
        -Path $dir `
        -NuGetApiKey ($nugetCredential.Password | ConvertFrom-SecureString -AsPlainText) `
        @commonParam
}

