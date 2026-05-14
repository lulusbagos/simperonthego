param(
    [ValidateSet("Basic", "Extended")]
    [string]$Level = "Basic",
    [string]$BaseUrl = "http://localhost:5088",
    [string]$AdminUsername = "admin",
    [string]$AdminPassword = "Admin@123",
    [long]$CompanyAId = 1,
    [long]$CompanyBId = 2,
    [switch]$Json,
    [string]$JsonOutPath
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$targetScript = if ($Level -eq "Extended") {
    Join-Path $scriptDir "smoke-company-isolation-extended.ps1"
}
else {
    Join-Path $scriptDir "smoke-company-isolation.ps1"
}

if (-not (Test-Path $targetScript)) {
    Write-Host "SMOKE RUNNER FAIL: target script tidak ditemukan -> $targetScript" -ForegroundColor Red
    exit 1
}

Write-Host "Running company isolation smoke test level: $Level"
Write-Host "Target: $targetScript"

$capturedOutput = @(
    & powershell -ExecutionPolicy Bypass -File $targetScript `
        -BaseUrl $BaseUrl `
        -AdminUsername $AdminUsername `
        -AdminPassword $AdminPassword `
        -CompanyAId $CompanyAId `
        -CompanyBId $CompanyBId
)

$exitCode = $LASTEXITCODE
$capturedOutput | ForEach-Object { Write-Host $_ }

if ($Json) {
    $passedLines = @($capturedOutput | Where-Object { $_ -match "PASS" }).Count
    $failedLines = @($capturedOutput | Where-Object { $_ -match "FAIL" }).Count

    $payload = [PSCustomObject]@{
        timestampUtc = [DateTime]::UtcNow.ToString("o")
        level = $Level
        baseUrl = $BaseUrl
        targetScript = $targetScript
        companyAId = $CompanyAId
        companyBId = $CompanyBId
        success = ($exitCode -eq 0)
        exitCode = $exitCode
        passLineCount = $passedLines
        failLineCount = $failedLines
        output = $capturedOutput
    }

    $jsonText = $payload | ConvertTo-Json -Depth 6
    if ([string]::IsNullOrWhiteSpace($JsonOutPath)) {
        Write-Output $jsonText
    }
    else {
        $outDir = Split-Path -Parent $JsonOutPath
        if (-not [string]::IsNullOrWhiteSpace($outDir) -and -not (Test-Path $outDir)) {
            New-Item -ItemType Directory -Path $outDir -Force | Out-Null
        }

        $jsonText | Set-Content -Path $JsonOutPath -Encoding UTF8
        Write-Host "JSON result written: $JsonOutPath"
    }
}

exit $exitCode
