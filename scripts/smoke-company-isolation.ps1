param(
    [string]$BaseUrl = "http://localhost:5088",
    [string]$AdminUsername = "admin",
    [string]$AdminPassword = "Admin@123",
    [long]$CompanyAId = 1,
    [long]$CompanyBId = 2
)

$ErrorActionPreference = "Stop"

function Get-CsrfToken {
    param(
        [Parameter(Mandatory = $true)][string]$Html
    )

    $match = [regex]::Match($Html, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
    if (-not $match.Success) {
        throw "CSRF token tidak ditemukan di halaman."
    }

    return $match.Groups[1].Value
}

function Login-Session {
    param(
        [Parameter(Mandatory = $true)][string]$Username,
        [Parameter(Mandatory = $true)][string]$Password
    )

    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -WebSession $session
    $token = Get-CsrfToken -Html $loginPage.Content

    $body = @{
        Username = $Username
        Password = $Password
        __RequestVerificationToken = $token
    }

    $loginResponse = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -Method Post -WebSession $session -Body $body -MaximumRedirection 0 -ErrorAction SilentlyContinue
    if ($loginResponse.StatusCode -ne 302) {
        throw "Login gagal untuk user '$Username'. Status: $($loginResponse.StatusCode)"
    }

    return $session
}

function New-CompanyAdmin {
    param(
        [Parameter(Mandatory = $true)]$AdminSession,
        [Parameter(Mandatory = $true)][string]$Username,
        [Parameter(Mandatory = $true)][long]$CompanyId
    )

    $usersPage = Invoke-WebRequest -Uri "$BaseUrl/Admin/Users" -WebSession $AdminSession
    $token = Get-CsrfToken -Html $usersPage.Content

    $body = @{
        Username = $Username
        Password = "Admin@123"
        FullName = "Smoke $Username"
        Role = "CompanyAdmin"
        CompanyId = $CompanyId.ToString()
        __RequestVerificationToken = $token
    }

    $response = Invoke-WebRequest -Uri "$BaseUrl/Admin/Users" -Method Post -WebSession $AdminSession -Body $body -MaximumRedirection 0 -ErrorAction SilentlyContinue
    if ($response.StatusCode -ne 302) {
        throw "Gagal membuat CompanyAdmin '$Username' untuk company_id=$CompanyId. Status: $($response.StatusCode)"
    }
}

function New-Employee {
    param(
        [Parameter(Mandatory = $true)]$AdminSession,
        [Parameter(Mandatory = $true)][string]$Nrp,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][long]$CompanyId
    )

    $page = Invoke-WebRequest -Uri "$BaseUrl/Admin/Employees" -WebSession $AdminSession
    $token = Get-CsrfToken -Html $page.Content

    $body = @{
        Nrp = $Nrp
        EmployeeName = $Name
        CompanyId = $CompanyId.ToString()
        __RequestVerificationToken = $token
    }

    $response = Invoke-WebRequest -Uri "$BaseUrl/Admin/Employees" -Method Post -WebSession $AdminSession -Body $body -MaximumRedirection 0 -ErrorAction SilentlyContinue
    if ($response.StatusCode -ne 302) {
        throw "Gagal membuat employee '$Nrp' untuk company_id=$CompanyId. Status: $($response.StatusCode). Cek apakah company_id valid."
    }
}

function Assert-Isolation {
    param(
        [Parameter(Mandatory = $true)]$Session,
        [Parameter(Mandatory = $true)][string]$OwnMarker,
        [Parameter(Mandatory = $true)][string]$OtherMarker,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $page = Invoke-WebRequest -Uri "$BaseUrl/Admin/Employees" -WebSession $Session
    $content = $page.Content

    $hasOwn = $content -like "*$OwnMarker*"
    $hasOther = $content -like "*$OtherMarker*"

    if (-not $hasOwn) {
        throw "[$Label] data milik sendiri tidak terlihat: $OwnMarker"
    }

    if ($hasOther) {
        throw "[$Label] data lintas perusahaan masih terlihat: $OtherMarker"
    }

    Write-Host "[$Label] PASS - own visible, other hidden"
}

try {
    $health = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -UseBasicParsing
    if ($health.StatusCode -ne 200) {
        throw "Aplikasi tidak siap di $BaseUrl (status $($health.StatusCode))"
    }

    $stamp = Get-Date -Format "yyyyMMddHHmmss"
    $adminA = "smk_ca1_$stamp"
    $adminB = "smk_ca2_$stamp"
    $nrpA = "SMK_EMP_A_$stamp"
    $nrpB = "SMK_EMP_B_$stamp"

    Write-Host "Login as Administrator..."
    $adminSession = Login-Session -Username $AdminUsername -Password $AdminPassword

    Write-Host "Create CompanyAdmin users..."
    New-CompanyAdmin -AdminSession $adminSession -Username $adminA -CompanyId $CompanyAId
    New-CompanyAdmin -AdminSession $adminSession -Username $adminB -CompanyId $CompanyBId

    Write-Host "Create company-scoped employees..."
    New-Employee -AdminSession $adminSession -Nrp $nrpA -Name "Smoke Employee A" -CompanyId $CompanyAId
    New-Employee -AdminSession $adminSession -Nrp $nrpB -Name "Smoke Employee B" -CompanyId $CompanyBId

    Write-Host "Validate visibility for CompanyAdmin A..."
    $sessionA = Login-Session -Username $adminA -Password "Admin@123"
    Assert-Isolation -Session $sessionA -OwnMarker $nrpA -OtherMarker $nrpB -Label $adminA

    Write-Host "Validate visibility for CompanyAdmin B..."
    $sessionB = Login-Session -Username $adminB -Password "Admin@123"
    Assert-Isolation -Session $sessionB -OwnMarker $nrpB -OtherMarker $nrpA -Label $adminB

    Write-Host "SMOKE TEST PASS: isolasi antar perusahaan berjalan benar." -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "SMOKE TEST FAIL: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
