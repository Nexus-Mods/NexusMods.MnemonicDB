<#
.SYNOPSIS
    Builds and installs NexusMods.MnemonicDB projects to the local NuGet cache.
.DESCRIPTION
    This script builds and packages the NexusMods.MnemonicDB projects with 
    version 9999.0.0-localbuild, and installs them directly to the user's local NuGet cache.
.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Debug.
.EXAMPLE
    ./build-to-local-nuget-cache.ps1 -Configuration Release
#>

param (
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"   # Default to Debug if not specified
)

# Set the current directory to the script directory
$ScriptDir = $PSScriptRoot
if (-not $ScriptDir) {
    # Fallback for legacy PowerShell
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
}
Write-Host "Setting current directory to script location: $ScriptDir" -ForegroundColor Cyan
Set-Location -Path $ScriptDir

# Define the version
$Version = "9999.0.0-localbuild"

# Get the user's home directory in a cross-platform way
$HomeDir = if ($IsWindows -or $null -eq $IsWindows) {
    $env:USERPROFILE
} else {
    $env:HOME
}

# Define the NuGet packages path
$NuGetBasePath = Join-Path -Path $HomeDir -ChildPath ".nuget" | Join-Path -ChildPath "packages"

# Define the projects to build
$Projects = @(
    "src/NexusMods.HyperDuck/NexusMods.HyperDuck.csproj",
    "src/NexusMods.MnemonicDB/NexusMods.MnemonicDB.csproj",
    "src/NexusMods.MnemonicDB.Abstractions/NexusMods.MnemonicDB.Abstractions.csproj",
    "src/NexusMods.MnemonicDB.SourceGenerator/NexusMods.MnemonicDB.SourceGenerator.csproj"
)

# Keep track of built packages
$BuiltPackages = @()

# Process each project
$OverallSuccess = $true
foreach ($Project in $Projects) {
    $ProjectName = [System.IO.Path]::GetFileNameWithoutExtension($Project)
    $PackageLowerName = $ProjectName.ToLower()
    $VersionPath = Join-Path -Path $NuGetBasePath -ChildPath $PackageLowerName | Join-Path -ChildPath $Version
    
    Write-Host "Processing $ProjectName in $Configuration configuration..." -ForegroundColor Cyan
    
    # Remove existing package if it exists
    if (Test-Path $VersionPath) {
        Write-Host "Removing existing $ProjectName version $Version..." -ForegroundColor Yellow
        try {
            Remove-Item $VersionPath -Recurse -Force
            Write-Host "Successfully removed existing package version." -ForegroundColor Green
        }
        catch {
            Write-Error "Failed to remove existing package. Error: $_"
            Write-Host "Files appear to be in use. Close any IDEs or kill dotnet.exe processes." -ForegroundColor Red
            $OverallSuccess = $false
            break
        }
    }
    
    # Create the package directory
    New-Item -Path $VersionPath -ItemType Directory -Force | Out-Null
    
    # Clean and pack directly to the NuGet cache
    dotnet clean $Project
    
    Write-Host "Building and packing $ProjectName..." -ForegroundColor Cyan
    dotnet pack $Project --configuration $Configuration /p:PackageVersion=$Version --output $VersionPath
    
    if ($LastExitCode -ne 0) {
        Write-Error "Failed to pack $ProjectName"
        $OverallSuccess = $false
        break
    }
    
    # Find the .nupkg file
    $NupkgFile = Get-ChildItem -Path $VersionPath -Filter "*.nupkg" | Select-Object -First 1
    
    if ($null -eq $NupkgFile) {
        Write-Error "Failed to find .nupkg file for $ProjectName"
        $OverallSuccess = $false
        break
    }
    
    # Extract in place
    try {
        Write-Host "Extracting package contents..." -ForegroundColor Cyan
        
        Rename-Item -Path $NupkgFile.FullName -NewName "$($NupkgFile.FullName).zip" -Force
        
        # Ensure both pwsh and legacy PowerShell work for extract
        if ($PSVersionTable.PSVersion.Major -ge 5) {
            Expand-Archive -Path "$($NupkgFile.FullName).zip" -DestinationPath $VersionPath -Force
        } else {
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::ExtractToDirectory($NupkgFile.FullName, $VersionPath, $true)
        }
        
        # Copy the original .nupkg file to the directory for NuGet to recognize it properly
        # This is crucial for packages with runtime assets
        Copy-Item -Path "$($NupkgFile.FullName).zip" -Destination $NupkgFile.FullName -Force
        
        # Write the .nupkg.metadata file to make the package recognized by NuGet
        # You think `contentHash` is used? Think again, muahahahaha~
        $metadataContent = @"
{
  "version": 2,
  "contentHash": "e900dFK7jHJ2WcprLcgJYQoOMc6ejRTwAAMi0VGOFbSczcF98ZDaqwoQIiyqpAwnja59FSbV+GUUXfc3vaQ2Jg==",
  "source": "https://api.nuget.org/v3/index.json"
}
"@
        $metadataPath = Join-Path -Path $VersionPath -ChildPath ".nupkg.metadata"
        Set-Content -Path $metadataPath -Value $metadataContent -Force
        
        Write-Host "$ProjectName has been installed to local NuGet cache." -ForegroundColor Green
        
        # Add to the list of built packages
        $BuiltPackages += $ProjectName
    }
    catch {
        Write-Error "Failed to extract package. Error: $_"
        $OverallSuccess = $false
        break
    }
}

if ($OverallSuccess) {
    Write-Host "All projects have been built and installed to the local NuGet cache." -ForegroundColor Green
    Write-Host "NuGet packages location: $NuGetBasePath" -ForegroundColor Green
    
    # Print Central Package Management entries
    Write-Host "`nCentral Package Management entries for your Directory.Packages.props:" -ForegroundColor Green
    Write-Host "----------------------------------------------------------------------" -ForegroundColor Green
    foreach ($Package in $BuiltPackages) {
        Write-Host "    <PackageVersion Include=""$Package"" Version=""$Version"" />"
    }
    Write-Host ""
    Write-Host "Copy these lines to your Directory.Packages.props file to use the local builds." -ForegroundColor Cyan
} else {
    Write-Host "Build and installation process failed. Please fix the issues and try again." -ForegroundColor Red
    exit 1
}