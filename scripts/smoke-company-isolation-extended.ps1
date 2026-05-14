param(
    [string]$BaseUrl = "http://localhost:5088",
    [string]$AdminUsername = "admin",
    [string]$AdminPassword = "Admin@123",
    [long]$CompanyAId = 1,
    [long]$CompanyBId = 2
)

$ErrorActionPreference = "Stop"

function Get-CsrfToken {
    param([string]$Html)
    $match = [regex]::Match($Html, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
    if (-not $match.Success) { throw "CSRF token tidak ditemukan." }
    return $match.Groups[1].Value
}

function Login-Session {
    param([string]$Username, [string]$Password)
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -WebSession $session
    $token = Get-CsrfToken -Html $loginPage.Content

    $body = @{ Username = $Username; Password = $Password; __RequestVerificationToken = $token }
    $resp = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -Method Post -WebSession $session -Body $body -MaximumRedirection 0 -ErrorAction SilentlyContinue
    if ($resp.StatusCode -ne 302) { throw "Login gagal untuk user '$Username'." }
    return $session
}

function Post-FormWithToken {
    param(
        [Parameter(Mandatory = $true)]$Session,
        [Parameter(Mandatory = $true)][string]$PageUrl,
        [Parameter(Mandatory = $true)][string]$PostUrl,
        [Parameter(Mandatory = $true)][hashtable]$Fields
    )

    $page = Invoke-WebRequest -Uri $PageUrl -WebSession $Session
    $token = Get-CsrfToken -Html $page.Content
    $body = @{} + $Fields
    $body["__RequestVerificationToken"] = $token

    $resp = Invoke-WebRequest -Uri $PostUrl -Method Post -WebSession $Session -Body $body -MaximumRedirection 0 -ErrorAction SilentlyContinue
    if ($resp.StatusCode -ne 302) {
        throw "POST gagal ke $PostUrl. Status: $($resp.StatusCode)"
    }
}

function Create-CompanyAdmin {
    param($AdminSession, [string]$Username, [long]$CompanyId)
    Post-FormWithToken -Session $AdminSession -PageUrl "$BaseUrl/Admin/Users" -PostUrl "$BaseUrl/Admin/Users" -Fields @{
        Username = $Username
        Password = "Admin@123"
        FullName = "Smoke $Username"
        Role = "CompanyAdmin"
        CompanyId = $CompanyId.ToString()
    }
}

function Create-Employee {
    param($AdminSession, [string]$Nrp, [string]$Name, [long]$CompanyId)
    Post-FormWithToken -Session $AdminSession -PageUrl "$BaseUrl/Admin/Employees" -PostUrl "$BaseUrl/Admin/Employees" -Fields @{
        Nrp = $Nrp
        EmployeeName = $Name
        CompanyId = $CompanyId.ToString()
    }
}

function Create-Vehicle {
    param($AdminSession, [string]$VehicleName, [string]$SimperType, [long]$CompanyId)
    Post-FormWithToken -Session $AdminSession -PageUrl "$BaseUrl/Admin/Vehicles" -PostUrl "$BaseUrl/Admin/Vehicles" -Fields @{
        CompanyId = $CompanyId.ToString()
        VehicleName = $VehicleName
        SimperType = $SimperType
    }
}

function Get-VehicleIdByNrp {
    param([string]$Nrp, [string]$VehicleName)
    $page = Invoke-WebRequest -Uri "$BaseUrl/Exam/SelectExam?nrp=$([uri]::EscapeDataString($Nrp))"
    $escaped = [regex]::Escape($VehicleName)
    $pattern = '<option value="(\d+)">' + $escaped
    $match = [regex]::Match($page.Content, $pattern)
    if (-not $match.Success) {
        throw "Vehicle '$VehicleName' tidak ditemukan di SelectExam untuk NRP '$Nrp'."
    }

    return [long]$match.Groups[1].Value
}

function Create-Question {
    param($AdminSession, [long]$CompanyId, [long]$VehicleId, [string]$QuestionMarker)
    Post-FormWithToken -Session $AdminSession -PageUrl "$BaseUrl/Admin/Questions" -PostUrl "$BaseUrl/Admin/Questions" -Fields @{
        CompanyId = $CompanyId.ToString()
        VehicleId = $VehicleId.ToString()
        QuestionText = "Q_MARKER_$QuestionMarker"
        OptionA = "A"
        OptionB = "B"
        OptionC = "C"
        OptionD = "D"
        CorrectAnswer = "A"
        Difficulty = "easy"
    }
}

function Run-ExamToResult {
    param([string]$Nrp, [long]$VehicleId)

    $publicSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession

    $step = Invoke-WebRequest -Uri "$BaseUrl/Exam/SelectExam?nrp=$([uri]::EscapeDataString($Nrp))" -WebSession $publicSession
    $selectToken = Get-CsrfToken -Html $step.Content
    $createBody = @{ Nrp = $Nrp; VehicleId = $VehicleId.ToString(); __RequestVerificationToken = $selectToken }
    $generated = Invoke-WebRequest -Uri "$BaseUrl/Exam/CreateSession" -Method Post -Body $createBody -WebSession $publicSession -ErrorAction SilentlyContinue
    if ($generated.StatusCode -ne 200) {
        throw "CreateSession gagal untuk '$Nrp' (status $($generated.StatusCode))."
    }

    $linkMatch = [regex]::Match($generated.Content, 'id="examLink" class="form-control" value="([^"]+)"')
    $pwdMatch = [regex]::Match($generated.Content, 'id="examPassword" class="form-control" value="([^"]+)"')
    if (-not $linkMatch.Success -or -not $pwdMatch.Success) {
        throw "SessionGenerated tidak valid untuk '$Nrp' (kemungkinan soal belum tersedia untuk vehicle terkait)."
    }

    $examLink = $linkMatch.Groups[1].Value
    $password = $pwdMatch.Groups[1].Value
    $tokenMatch = [regex]::Match($examLink, 'token=([^&]+)')
    if (-not $tokenMatch.Success) { throw "Token ujian tidak ditemukan." }
    $examToken = [uri]::UnescapeDataString($tokenMatch.Groups[1].Value)

    $accessPage = Invoke-WebRequest -Uri "$BaseUrl/Exam/Access?token=$([uri]::EscapeDataString($examToken))" -WebSession $publicSession
    $accessToken = Get-CsrfToken -Html $accessPage.Content
    $accessBody = @{ Token = $examToken; Password = $password; __RequestVerificationToken = $accessToken }
    $accessResp = Invoke-WebRequest -Uri "$BaseUrl/Exam/Access" -Method Post -WebSession $publicSession -Body $accessBody -MaximumRedirection 0 -ErrorAction SilentlyContinue
    if ($accessResp.StatusCode -ne 302) {
        throw "Access exam gagal untuk token '$examToken'."
    }

    $startPage = Invoke-WebRequest -Uri "$BaseUrl/Exam/Start?token=$([uri]::EscapeDataString($examToken))" -WebSession $publicSession
    $submitToken = Get-CsrfToken -Html $startPage.Content

    $camResp = Invoke-WebRequest -Uri "$BaseUrl/Exam/CameraStatus" -Method Post -WebSession $publicSession -ContentType "application/json" -Body (@{ token = $examToken; isActive = $true } | ConvertTo-Json -Compress) -ErrorAction SilentlyContinue
    if ($camResp.StatusCode -ne 200) {
        throw "CameraStatus gagal untuk token '$examToken' (status $($camResp.StatusCode))."
    }

    $submitBody = @{ token = $examToken; __RequestVerificationToken = $submitToken }
    $submitResult = Invoke-WebRequest -Uri "$BaseUrl/Exam/Submit" -Method Post -WebSession $publicSession -Body $submitBody -ErrorAction SilentlyContinue
    if ($submitResult.StatusCode -ne 200) {
        throw "Submit gagal untuk token '$examToken' (status $($submitResult.StatusCode))."
    }

    if ($submitResult.Content -notlike "*Exam Completed*") {
        throw "Submit exam tidak menghasilkan halaman result yang valid untuk NRP '$Nrp'."
    }
}

function Assert-ContentIsolation {
    param($Session, [string]$Url, [string]$OwnMarker, [string]$OtherMarker, [string]$Label)

    $page = Invoke-WebRequest -Uri $Url -WebSession $Session
    $hasOwn = $page.Content -like "*$OwnMarker*"
    $hasOther = $page.Content -like "*$OtherMarker*"

    if (-not $hasOwn) { throw "[$Label] marker sendiri tidak ditemukan di ${Url}: $OwnMarker" }
    if ($hasOther) { throw "[$Label] marker lintas perusahaan terlihat di ${Url}: $OtherMarker" }

    Write-Host "[$Label] PASS $Url"
}

try {
    $health = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -UseBasicParsing
    if ($health.StatusCode -ne 200) { throw "Aplikasi tidak siap di $BaseUrl" }

    $stamp = Get-Date -Format "yyyyMMddHHmmss"
    $adminA = "smk2_ca1_$stamp"
    $adminB = "smk2_ca2_$stamp"
    $nrpA = "SMK2_EMP_A_$stamp"
    $nrpB = "SMK2_EMP_B_$stamp"
    $vehA = "SMK2_VEH_A_$stamp"
    $vehB = "SMK2_VEH_B_$stamp"
    $qA = "SMK2_QA_$stamp"
    $qB = "SMK2_QB_$stamp"

    Write-Host "Login administrator..."
    $adminSession = Login-Session -Username $AdminUsername -Password $AdminPassword

    Write-Host "Create scoped admins, employees, and vehicles..."
    Create-CompanyAdmin -AdminSession $adminSession -Username $adminA -CompanyId $CompanyAId
    Create-CompanyAdmin -AdminSession $adminSession -Username $adminB -CompanyId $CompanyBId
    Create-Employee -AdminSession $adminSession -Nrp $nrpA -Name "Smoke2 Employee A" -CompanyId $CompanyAId
    Create-Employee -AdminSession $adminSession -Nrp $nrpB -Name "Smoke2 Employee B" -CompanyId $CompanyBId
    Create-Vehicle -AdminSession $adminSession -VehicleName $vehA -SimperType "TypeA" -CompanyId $CompanyAId
    Create-Vehicle -AdminSession $adminSession -VehicleName $vehB -SimperType "TypeB" -CompanyId $CompanyBId

    Write-Host "Resolve vehicle IDs and create question markers..."
    $vehAId = Get-VehicleIdByNrp -Nrp $nrpA -VehicleName $vehA
    $vehBId = Get-VehicleIdByNrp -Nrp $nrpB -VehicleName $vehB
    Create-Question -AdminSession $adminSession -CompanyId $CompanyAId -VehicleId $vehAId -QuestionMarker $qA
    Create-Question -AdminSession $adminSession -CompanyId $CompanyBId -VehicleId $vehBId -QuestionMarker $qB

    Write-Host "Generate results through exam flow..."
    Run-ExamToResult -Nrp $nrpA -VehicleId $vehAId
    Run-ExamToResult -Nrp $nrpB -VehicleId $vehBId

    Write-Host "Validate company A isolation on Questions and Results..."
    $sessionA = Login-Session -Username $adminA -Password "Admin@123"
    Assert-ContentIsolation -Session $sessionA -Url "$BaseUrl/Admin/Questions" -OwnMarker $qA -OtherMarker $qB -Label $adminA
    Assert-ContentIsolation -Session $sessionA -Url "$BaseUrl/Admin/Results?nrp=$([uri]::EscapeDataString($nrpA))" -OwnMarker $nrpA -OtherMarker $nrpB -Label $adminA

    Write-Host "Validate company B isolation on Questions and Results..."
    $sessionB = Login-Session -Username $adminB -Password "Admin@123"
    Assert-ContentIsolation -Session $sessionB -Url "$BaseUrl/Admin/Questions" -OwnMarker $qB -OtherMarker $qA -Label $adminB
    Assert-ContentIsolation -Session $sessionB -Url "$BaseUrl/Admin/Results?nrp=$([uri]::EscapeDataString($nrpB))" -OwnMarker $nrpB -OtherMarker $nrpA -Label $adminB

    Write-Host "SMOKE EXTENDED PASS: isolasi Questions + Results antar perusahaan valid." -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "SMOKE EXTENDED FAIL: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
