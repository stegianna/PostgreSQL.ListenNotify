# run-coverage.ps1

$ErrorActionPreference = "Stop"

# Paths
$testsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$testProject = "$testsDir\PostgreSQL.ListenNotify.Tests\PostgreSQL.ListenNotify.Tests.csproj"   # <-- Change this to your actual test project path
$outputDir = "$testsDir\TestResults"
$coverageFile = "$outputDir\coverage.cobertura.xml"
$reportDir = "$testsDir\CoverageReport"

# Make sure output folders exist
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

# Optional: Ensure ReportGenerator is installed
if (-not (Get-Command "reportgenerator" -ErrorAction SilentlyContinue)) {
    Write-Host "Installing ReportGenerator..."
    dotnet tool install --global dotnet-reportgenerator-globaltool
    $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
}

# Run tests with Coverlet coverage enabled, excluding test projects
Write-Host "Running tests and collecting coverage..."

dotnet test $testProject `
    /p:CollectCoverage=true `
    /p:CoverletOutput=$outputDir\coverage `
    /p:CoverletOutputFormat=cobertura `
    /p:Exclude="[*.Tests*]*"

# Generate HTML report
Write-Host "Generating HTML report..."

reportgenerator `
    -reports:$coverageFile `
    -targetdir:$reportDir `
    -reporttypes:Html

Write-Host "`Coverage report generated at: $reportDir\index.html"