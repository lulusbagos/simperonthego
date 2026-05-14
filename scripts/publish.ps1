param(
    [string]$Configuration = "Release",
    [string]$Output = ".\\artifacts\\publish"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot

$env:DOTNET_CLI_HOME = $projectRoot
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

Write-Host "Publishing application..." -ForegroundColor Cyan
dotnet publish "$projectRoot\\SimperSecureOnlineTestSystem.csproj" `
    -c $Configuration `
    --output "$projectRoot\\$Output" `
    /p:UseAppHost=false
