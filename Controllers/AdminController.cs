using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QRCoder;
using QuestPDF.Fluent;
using SimperSecureOnlineTestSystem.Application.DTOs;
using SimperSecureOnlineTestSystem.Application.Services;
using SimperSecureOnlineTestSystem.Domain.Entities;
using SimperSecureOnlineTestSystem.Domain.Enums;
using SimperSecureOnlineTestSystem.Infrastructure.Documents;
using SimperSecureOnlineTestSystem.ViewModels;

namespace SimperSecureOnlineTestSystem.Controllers;

[Authorize(Policy = "AdminAccess")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IExamService _examService;
    private readonly IWebHostEnvironment _environment;

    public AdminController(IAdminService adminService, IExamService examService, IWebHostEnvironment environment)
    {
        _adminService = adminService;
        _examService = examService;
        _environment = environment;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        if (User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var isAdministrator = User.IsInRole(SystemUserRole.Administrator);
        var isCompanyAdmin = User.IsInRole(SystemUserRole.CompanyAdmin);
        var hasValidCompanyClaim = long.TryParse(User.FindFirstValue("company_id"), out var companyId) && companyId > 0;

        if (!isAdministrator && isCompanyAdmin && !hasValidCompanyClaim)
        {
            context.Result = Forbid();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var active = await _adminService.GetActiveExamMonitorsAsync(BuildScope(), cancellationToken);
        var companySummaries = active
            .GroupBy(x => new { x.CompanyId, x.CompanyName })
            .Select(group => new DashboardCompanySummaryViewModel
            {
                CompanyId = group.Key.CompanyId,
                CompanyName = group.Key.CompanyName,
                ActiveExamCount = group.Count(),
                CameraOffCount = group.Count(x => !x.CameraActive),
                SuspiciousCount = group.Count(x => x.TabSwitchCount > 0),
                AverageScore = group.Any() ? Math.Round(group.Average(x => x.Score), 2) : 0
            })
            .OrderBy(x => x.CompanyName)
            .ToList();

        return View(new AdminDashboardViewModel
        {
            ActiveExams = active,
            CompanySummaries = companySummaries,
            IsAdministrator = User.IsInRole(SystemUserRole.Administrator)
        });
    }

    [HttpGet]
    [Authorize(Roles = SystemUserRole.Administrator)]
    public IActionResult Companies(CancellationToken cancellationToken)
    {
        TempData["FlashInfo"] = "Master company berasal dari sistem utama melalui vw_company dan tidak dikelola dari aplikasi ini.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [Authorize(Roles = SystemUserRole.Administrator)]
    [ValidateAntiForgeryToken]
    public IActionResult Companies(CompanyManageViewModel model, CancellationToken cancellationToken)
    {
        TempData["CompanyError"] = "Master company tidak dapat diubah dari aplikasi ini. Sumber data menggunakan vw_company.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [Authorize(Roles = SystemUserRole.Administrator)]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateCompany(CompanyManageViewModel model, CancellationToken cancellationToken)
    {
        TempData["CompanyError"] = "Master company tidak dapat diperbarui dari aplikasi ini. Sumber data menggunakan vw_company.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [Authorize(Roles = SystemUserRole.Administrator)]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteCompany(long companyId, CancellationToken cancellationToken)
    {
        TempData["CompanyError"] = "Master company tidak dapat dihapus dari aplikasi ini. Sumber data menggunakan vw_company.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> ActiveExams(CancellationToken cancellationToken)
    {
        var active = await _adminService.GetActiveExamMonitorsAsync(BuildScope(), cancellationToken);
        return Json(active);
    }

    [HttpGet]
    public async Task<IActionResult> Results(string? keyword, string? nrp, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken)
    {
        var scope = BuildScope();
        var searchTerm = string.IsNullOrWhiteSpace(keyword) ? nrp : keyword;
        var theoryResults = await _adminService.SearchResultsByNrpAsync(scope, searchTerm, cancellationToken);
        var practicalSessions = await _adminService.GetPracticalSessionsAsync(scope, cancellationToken);
        var employees = await _adminService.GetEmployeesAsync(scope, cancellationToken);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            practicalSessions = practicalSessions
                .Where(x =>
                    x.Nrp.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.EmployeeName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.VehicleName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.CompanyName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.InstructorName.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();

            employees = employees
                .Where(x =>
                    x.Nrp.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.EmployeeName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.Company?.CompanyName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        var companyByNrp = employees
            .Where(x => !string.IsNullOrWhiteSpace(x.Nrp))
            .GroupBy(x => x.Nrp.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(x => x.Company?.CompanyName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "-",
                StringComparer.OrdinalIgnoreCase);

        var latestTheory = theoryResults
            .GroupBy(x => $"{x.Nrp.Trim().ToUpperInvariant()}|{x.VehicleId}")
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(x => x.FinishedAt).First());

        var latestPractical = practicalSessions
            .GroupBy(x => $"{x.Nrp.Trim().ToUpperInvariant()}|{x.VehicleId}")
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(x => x.SubmittedAt ?? x.ScheduledAt).First());

        var orderedKeys = latestTheory.Keys
            .Union(latestPractical.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = orderedKeys
            .Select(key =>
            {
                latestTheory.TryGetValue(key, out var theory);
                latestPractical.TryGetValue(key, out var practical);

                var nrpValue = practical?.Nrp ?? theory?.Nrp ?? string.Empty;
                var employeeName = practical?.EmployeeName ?? theory?.EmployeeName ?? string.Empty;
                var vehicleId = practical?.VehicleId ?? theory?.VehicleId;
                var vehicleName = practical?.VehicleName ?? theory?.VehicleName ?? "-";
                var companyName = practical?.CompanyName
                    ?? (companyByNrp.TryGetValue(nrpValue.Trim(), out var company) ? company : "-");

                return new FinalStatusRowViewModel
                {
                    Nrp = nrpValue,
                    EmployeeName = employeeName,
                    CompanyName = companyName,
                    VehicleId = vehicleId,
                    VehicleName = vehicleName,
                    TheoryScore = theory?.Score,
                    TheoryPassStatus = theory?.PassStatus,
                    TheoryFinishedAt = theory?.FinishedAt,
                    TheorySessionId = theory?.SessionId,
                    PracticalNumericScore = practical?.FinalNumericScore,
                    PracticalGrade = practical?.FinalGrade,
                    PracticalPassStatus = practical?.PassStatus,
                    PracticalFinishedAt = practical?.SubmittedAt ?? practical?.ScheduledAt,
                    PracticalSessionId = practical?.Id,
                    InstructorName = practical?.InstructorName
                };
            })
            .OrderBy(x => x.Nrp)
            .ThenBy(x => x.VehicleName)
            .ToList();

        if (dateFrom.HasValue)
        {
            var fromDate = dateFrom.Value.Date;
            rows = rows
                .Where(x => x.LatestActivityAt.HasValue && x.LatestActivityAt.Value.ToLocalTime().Date >= fromDate)
                .ToList();
        }

        if (dateTo.HasValue)
        {
            var toDate = dateTo.Value.Date;
            rows = rows
                .Where(x => x.LatestActivityAt.HasValue && x.LatestActivityAt.Value.ToLocalTime().Date <= toDate)
                .ToList();
        }

        var vm = new FinalStatusViewModel
        {
            IsAdministrator = scope.IsAdministrator,
            SearchTerm = searchTerm,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Rows = rows,
            TotalRows = rows.Count,
            CompleteRows = rows.Count(x => x.TheoryPassStatus.HasValue && x.PracticalPassStatus.HasValue),
            PassedRows = rows.Count(x => x.FinalStatus == "LULUS"),
            FailedRows = rows.Count(x => x.FinalStatus == "TIDAK LULUS"),
            InProgressRows = rows.Count(x => x.FinalStatus == "PROSES" || x.FinalStatus == "BELUM MULAI")
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> PracticalResults(string? keyword, CancellationToken cancellationToken)
    {
        TempData["FlashInfo"] = "Hasil praktek sekarang digabung ke Status Akhir Peserta untuk workflow yang lebih sederhana.";
        return RedirectToAction(nameof(Results), new { keyword });
    }

    [HttpGet]
    public async Task<IActionResult> Instructors(string? keyword, CancellationToken cancellationToken)
    {
        var scope = BuildScope();
        var instructors = await _adminService.GetInstructorsAsync(scope, cancellationToken);
        var sessions = await _adminService.GetPracticalSessionsAsync(scope, cancellationToken);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            instructors = instructors
                .Where(x =>
                    x.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Username.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.Company != null && x.Company.CompanyName.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var allowedInstructorIds = instructors.Select(x => x.Id).ToHashSet();
            sessions = sessions
                .Where(x =>
                    allowedInstructorIds.Contains(x.InstructorUserId) ||
                    x.InstructorName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.EmployeeName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Nrp.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.VehicleName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.CompanyName.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return View(new InstructorDirectoryViewModel
        {
            IsAdministrator = scope.IsAdministrator,
            SearchTerm = keyword,
            Instructors = instructors,
            Sessions = sessions
        });
    }

    [HttpGet]
    public async Task<IActionResult> AnswerReview(long sessionId, CancellationToken cancellationToken)
    {
        var review = await _adminService.GetAnswerReviewAsync(BuildScope(), sessionId, cancellationToken);
        if (review is null)
        {
            return NotFound();
        }

        return View(review);
    }

    [HttpGet]
    public async Task<IActionResult> ExportResults(string? nrp, CancellationToken cancellationToken)
    {
        var csv = await _adminService.ExportResultsCsvAsync(BuildScope(), nrp, cancellationToken);
        return File(csv, "text/csv", $"simper-results-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> SummaryScore(long theorySessionId, long practicalSessionId, CancellationToken cancellationToken)
    {
        var summary = await BuildSummaryScoreViewModelAsync(theorySessionId, practicalSessionId, cancellationToken);
        if (summary is null)
        {
            return NotFound();
        }

        if (!summary.IsComplete)
        {
            return BadRequest("Hasil teori dan praktek belum lengkap.");
        }

        return View(summary);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadSummaryScorePdf(long theorySessionId, long practicalSessionId, CancellationToken cancellationToken)
    {
        var summary = await BuildSummaryScoreViewModelAsync(theorySessionId, practicalSessionId, cancellationToken);
        if (summary is null)
        {
            return NotFound();
        }

        if (!summary.IsComplete)
        {
            return BadRequest("Dokumen PDF hanya tersedia setelah hasil teori dan praktek lengkap.");
        }

        var document = new SummaryScorePdfDocument(summary);
        var pdfBytes = document.GeneratePdf();
        var fileDate = summary.PracticalFinishedAt
            ?? summary.TheoryFinishedAt
            ?? summary.PracticalScheduledAt;
        var safeNrp = SanitizeFileToken(summary.Nrp, "participant");
        var fileName = $"summary-score-{safeNrp}-{fileDate.ToLocalTime():yyyyMMdd}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Employees(string? searchTerm, int page = 1, CancellationToken cancellationToken = default)
    {
        var scope = BuildScope();
        const int pageSize = 50;
        var totalApplicants = await _adminService.CountSimperApplicantsAsync(scope, searchTerm, cancellationToken);
        var applicants = await _adminService.GetSimperApplicantsAsync(scope, searchTerm, page, pageSize, cancellationToken);
        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            applicants = applicants.Where(x => x.CreatedCompanyId == scope.CompanyId).ToList();
        }

        var vm = new EmployeeManageViewModel
        {
            ExistingCompanies = await _adminService.GetCompaniesAsync(scope, cancellationToken),
            EligibleApplicants = applicants,
            SearchTerm = searchTerm,
            Page = page <= 0 ? 1 : page,
            PageSize = pageSize,
            TotalApplicants = totalApplicants,
            IsAdministrator = scope.IsAdministrator
        };

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            vm.CompanyId = scope.CompanyId!.Value;
        }
        else
        {
            vm.CompanyId = vm.ExistingCompanies.FirstOrDefault()?.Id ?? 0;
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Employees(EmployeeManageViewModel model, CancellationToken cancellationToken)
    {
        var scope = BuildScope();
        const int pageSize = 50;
        model.ExistingCompanies = await _adminService.GetCompaniesAsync(scope, cancellationToken);
        model.TotalApplicants = await _adminService.CountSimperApplicantsAsync(scope, model.SearchTerm, cancellationToken);
        model.EligibleApplicants = await _adminService.GetSimperApplicantsAsync(scope, model.SearchTerm, model.Page, pageSize, cancellationToken);
        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            model.EligibleApplicants = model.EligibleApplicants.Where(x => x.CreatedCompanyId == scope.CompanyId).ToList();
        }
        model.PageSize = pageSize;
        model.IsAdministrator = scope.IsAdministrator;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            model.CompanyId = scope.CompanyId!.Value;
        }

        var employee = new Employee
        {
            Nrp = model.Nrp,
            EmployeeName = model.EmployeeName,
            CompanyId = model.CompanyId
        };

        await _adminService.AddEmployeeAsync(employee, cancellationToken);
        return RedirectToAction(nameof(Employees));
    }

    [HttpGet]
    public async Task<IActionResult> Vehicles(CancellationToken cancellationToken)
    {
        var scope = BuildScope();
        var vm = new VehicleManageViewModel
        {
            ExistingCompanies = await _adminService.GetCompaniesAsync(scope, cancellationToken),
            ExistingVehicles = await _adminService.GetVehiclesAsync(scope, cancellationToken),
            IsAdministrator = scope.IsAdministrator
        };

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            vm.CompanyId = scope.CompanyId!.Value;
        }
        else
        {
            vm.CompanyId = vm.ExistingCompanies.FirstOrDefault()?.Id ?? 0;
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vehicles(VehicleManageViewModel model, CancellationToken cancellationToken)
    {
        var scope = BuildScope();
        model.ExistingCompanies = await _adminService.GetCompaniesAsync(scope, cancellationToken);
        model.ExistingVehicles = await _adminService.GetVehiclesAsync(scope, cancellationToken);
        model.IsAdministrator = scope.IsAdministrator;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            model.CompanyId = scope.CompanyId!.Value;
        }

        var vehicle = new Vehicle
        {
            CompanyId = model.CompanyId,
            VehicleName = model.VehicleName,
            SimperType = model.SimperType
        };

        await _adminService.AddVehicleAsync(vehicle, cancellationToken);
        return RedirectToAction(nameof(Vehicles));
    }

    [HttpGet]
    public async Task<IActionResult> Questions(CancellationToken cancellationToken)
    {
        var scope = BuildScope();
        var companies = await _adminService.GetCompaniesAsync(scope, cancellationToken);
        var vehicles = await _adminService.GetVehiclesAsync(scope, cancellationToken);

        var vm = new QuestionManageViewModel
        {
            ExistingQuestions = await _adminService.GetQuestionsAsync(scope, cancellationToken),
            ExistingCompanies = companies,
            ExistingVehicles = vehicles
        };

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            vm.CompanyId = scope.CompanyId!.Value;
            vm.ImportCompanyId = scope.CompanyId;
        }
        else
        {
            vm.ImportCompanyId = companies.FirstOrDefault()?.Id;
        }

        vm.VehicleId = vehicles.FirstOrDefault()?.Id ?? 0;
        vm.ImportVehicleId = vehicles.FirstOrDefault()?.Id;

        return View(vm);
    }

    [HttpGet]
    public IActionResult DownloadQuestionTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Questions");

        ws.Cell(1, 1).Value = "CompanyName";
        ws.Cell(1, 2).Value = "VehicleName";
        ws.Cell(1, 3).Value = "QuestionText";
        ws.Cell(1, 4).Value = "OptionA";
        ws.Cell(1, 5).Value = "OptionB";
        ws.Cell(1, 6).Value = "OptionC";
        ws.Cell(1, 7).Value = "OptionD";
        ws.Cell(1, 8).Value = "CorrectAnswer";
        ws.Cell(1, 9).Value = "Difficulty";
        ws.Cell(1, 10).Value = "ImageUrl";
        ws.Cell(1, 11).Value = "VideoUrl";

        ws.Cell(2, 1).Value = "PT INDEXIM COALINDO";
        ws.Cell(2, 2).Value = "HD 785";
        ws.Cell(2, 3).Value = "Sebelum operasi HD 785, pemeriksaan utama adalah ...";
        ws.Cell(2, 4).Value = "Cek rem, lampu, ban, dan fluida";
        ws.Cell(2, 5).Value = "Langsung muat material";
        ws.Cell(2, 6).Value = "Matikan alarm unit";
        ws.Cell(2, 7).Value = "Lewati pre-start check";
        ws.Cell(2, 8).Value = "A";
        ws.Cell(2, 9).Value = "easy";
        ws.Cell(2, 10).Value = "";
        ws.Cell(2, 11).Value = "";

        ws.Cell(3, 1).Value = "PT INDEXIM COALINDO";
        ws.Cell(3, 2).Value = "LV";
        ws.Cell(3, 3).Value = "Saat berkendara LV di area tambang, operator wajib ...";
        ws.Cell(3, 4).Value = "Pakai seatbelt";
        ws.Cell(3, 5).Value = "Gunakan ponsel";
        ws.Cell(3, 6).Value = "Melewati batas kecepatan";
        ws.Cell(3, 7).Value = "Abaikan rambu";
        ws.Cell(3, 8).Value = "A";
        ws.Cell(3, 9).Value = "easy";
        ws.Cell(3, 10).Value = "";
        ws.Cell(3, 11).Value = "";

        var header = ws.Range(1, 1, 1, 11);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF5EF");
        ws.Columns(1, 11).AdjustToContents();
        ws.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"simper-question-template-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> Schedules(DateTime? date, int employeePage = 1, CancellationToken cancellationToken = default)
    {
        var selectedDate = date?.Date ?? DateTime.Today;
        var board = await _adminService.GetScheduleBoardAsync(BuildScope(), selectedDate, cancellationToken);
        const int employeePageSize = 40;
        board.TotalEmployeeCount = board.Employees.Count;
        board.EmployeePageSize = employeePageSize;
        board.EmployeePage = employeePage <= 0 ? 1 : employeePage;
        if (board.EmployeePage > board.TotalEmployeePages)
        {
            board.EmployeePage = board.TotalEmployeePages;
        }
        board.DisplayedEmployees = board.Employees
            .Skip((board.EmployeePage - 1) * employeePageSize)
            .Take(employeePageSize)
            .ToList();
        board.PracticalCompletedKeys = (await _adminService.GetPracticalSessionsAsync(BuildScope(), cancellationToken))
            .Where(x => x.Status == PracticalAssessmentStatus.Submitted)
            .Select(x => $"{x.EmployeeId}|{x.VehicleId}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return View(board);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoSchedule([FromForm] AutoScheduleRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _adminService.AutoScheduleAsync(BuildScope(), request, cancellationToken);
        TempData[result.Success ? "ScheduleSuccess" : "ScheduleError"] = result.Message;
        return RedirectToAction(nameof(Schedules), new { date = request.StartDate.Date });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScheduleByDateTime(
        [FromForm] long employeeId,
        [FromForm] long vehicleId,
        [FromForm] DateTime scheduleDate,
        [FromForm] string scheduleTime,
        CancellationToken cancellationToken)
    {
        if (employeeId <= 0 || vehicleId <= 0)
        {
            TempData["ScheduleError"] = "Pilih peserta dan vehicle terlebih dahulu.";
            return RedirectToAction(nameof(Schedules), new { date = scheduleDate.Date });
        }

        if (!TimeSpan.TryParse(scheduleTime, out var parsedTime))
        {
            TempData["ScheduleError"] = "Format jam tidak valid.";
            return RedirectToAction(nameof(Schedules), new { date = scheduleDate.Date });
        }

        var localSchedule = scheduleDate.Date.Add(parsedTime);
        var request = new ScheduleAssignRequestDto
        {
            EmployeeId = employeeId,
            VehicleId = vehicleId,
            ScheduledAt = localSchedule
        };

        if (long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            request.CreatedByUserId = userId;
        }

        var result = await _adminService.AssignScheduleAsync(BuildScope(), request, cancellationToken);
        TempData[result.Success ? "ScheduleSuccess" : "ScheduleError"] = result.Message;
        return RedirectToAction(nameof(Schedules), new { date = scheduleDate.Date });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateExamAccess(
        [FromForm] long employeeId,
        [FromForm] long vehicleId,
        [FromForm] DateTime? date,
        CancellationToken cancellationToken)
    {
        var selectedDate = date?.Date ?? DateTime.Today;
        if (employeeId <= 0 || vehicleId <= 0)
        {
            TempData["ScheduleError"] = "Pilih peserta dan jenis tes (vehicle) terlebih dahulu.";
            return RedirectToAction(nameof(Schedules), new { date = selectedDate });
        }

        var scope = BuildScope();
        var employees = await _adminService.GetEmployeesAsync(scope, cancellationToken);
        var vehicles = await _adminService.GetVehiclesAsync(scope, cancellationToken);

        var employee = employees.FirstOrDefault(x => x.Id == employeeId);
        var vehicle = vehicles.FirstOrDefault(x => x.Id == vehicleId);
        if (employee is null || vehicle is null)
        {
            TempData["ScheduleError"] = "Data peserta atau vehicle tidak ditemukan pada scope user login.";
            return RedirectToAction(nameof(Schedules), new { date = selectedDate });
        }

        if (scope.HasCompanyScope && employee.CompanyId != vehicle.CompanyId)
        {
            TempData["ScheduleError"] = "Peserta dan vehicle harus berasal dari company yang sama.";
            return RedirectToAction(nameof(Schedules), new { date = selectedDate });
        }

        try
        {
            var session = await _examService.CreateSessionAsync(
                new SessionCreateRequestDto(employee.Nrp, vehicleId),
                cancellationToken);

            var link = Url.Action("Access", "Exam", new { refId = session.RefId }, Request.Scheme)
                ?? $"/Exam/Access?refId={session.RefId}";

            return View("~/Views/Exam/SessionGenerated.cshtml", new SessionGeneratedViewModel
            {
                Token = session.Token,
                RefId = session.RefId,
                AccessPassword = session.AccessPassword,
                ExamLink = link,
                EndTime = session.EndTime,
                EmployeeName = session.EmployeeName,
                VehicleName = session.VehicleName
            });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ScheduleError"] = ex.Message;
            return RedirectToAction(nameof(Schedules), new { date = selectedDate });
        }
    }

    [HttpGet]
    public async Task<IActionResult> PracticalTemplates(long? editId, CancellationToken cancellationToken)
    {
        var scope = BuildScope();
        var vm = new PracticalTemplateManageViewModel
        {
            ExistingCompanies = await _adminService.GetCompaniesAsync(scope, cancellationToken),
            ExistingVehicles = await _adminService.GetVehiclesAsync(scope, cancellationToken),
            ExistingTemplates = await _adminService.GetPracticalTemplatesAsync(scope, cancellationToken),
            IsAdministrator = scope.IsAdministrator,
            ScoringMode = PracticalScoringMode.Numeric,
            ItemDefinitions = "Persiapan|Pre-start inspection|30\r\nOperasional|Kontrol unit saat manuver|40\r\nSafety|Kepatuhan SOP keselamatan|30"
        };

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            vm.CompanyId = scope.CompanyId!.Value;
        }
        else
        {
            vm.CompanyId = vm.ExistingCompanies.FirstOrDefault()?.Id ?? 0;
        }

        vm.VehicleId = vm.ExistingVehicles.FirstOrDefault(x => x.CompanyId == vm.CompanyId)?.Id ?? vm.ExistingVehicles.FirstOrDefault()?.Id ?? 0;
        if (editId.HasValue)
        {
            var existingTemplate = vm.ExistingTemplates.FirstOrDefault(x => x.Id == editId.Value);
            if (existingTemplate is null)
            {
                TempData["PracticalError"] = "Template praktek tidak ditemukan pada scope login.";
                return RedirectToAction(nameof(PracticalTemplates));
            }

            vm.TemplateId = existingTemplate.Id;
            vm.CompanyId = existingTemplate.CompanyId;
            vm.VehicleId = existingTemplate.VehicleId;
            vm.TemplateName = existingTemplate.TemplateName;
            vm.ScoringMode = existingTemplate.ScoringMode;
            vm.PassingScore = existingTemplate.PassingScore;
            vm.PassingGrade = existingTemplate.PassingGrade;
            vm.GradeOptions = existingTemplate.GradeOptions ?? "A,B,C,D";
            vm.ItemDefinitions = string.Join(Environment.NewLine, existingTemplate.Items.Select(x => $"{x.SectionName}|{x.ItemLabel}|{x.Weight}"));
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PracticalTemplates(PracticalTemplateManageViewModel model, CancellationToken cancellationToken)
    {
        var scope = BuildScope();
        model.ExistingCompanies = await _adminService.GetCompaniesAsync(scope, cancellationToken);
        model.ExistingVehicles = await _adminService.GetVehiclesAsync(scope, cancellationToken);
        model.ExistingTemplates = await _adminService.GetPracticalTemplatesAsync(scope, cancellationToken);
        model.IsAdministrator = scope.IsAdministrator;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var request = new PracticalTemplateUpsertDto
            {
                CompanyId = model.CompanyId,
                VehicleId = model.VehicleId,
                TemplateName = model.TemplateName,
                ScoringMode = model.ScoringMode,
                PassingScore = model.PassingScore,
                PassingGrade = model.PassingGrade,
                GradeOptions = model.GradeOptions,
                Items = ParsePracticalTemplateItems(model.ItemDefinitions)
            };

            if (model.TemplateId.HasValue && model.TemplateId.Value > 0)
            {
                await _adminService.UpdatePracticalTemplateAsync(scope, model.TemplateId.Value, request, cancellationToken);
                TempData["PracticalSuccess"] = "Template penilaian praktek berhasil diperbarui.";
            }
            else
            {
                await _adminService.CreatePracticalTemplateAsync(scope, request, cancellationToken);
                TempData["PracticalSuccess"] = "Template penilaian praktek berhasil disimpan.";
            }

            return RedirectToAction(nameof(PracticalTemplates));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPracticalTemplateStatus(long templateId, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            await _adminService.SetPracticalTemplateStatusAsync(BuildScope(), templateId, isActive, cancellationToken);
            TempData["PracticalSuccess"] = isActive
                ? "Template praktek berhasil diaktifkan."
                : "Template praktek berhasil diarsipkan.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["PracticalError"] = ex.Message;
        }

        return RedirectToAction(nameof(PracticalTemplates));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePracticalTemplate(long templateId, CancellationToken cancellationToken)
    {
        try
        {
            await _adminService.DeletePracticalTemplateAsync(BuildScope(), templateId, cancellationToken);
            TempData["PracticalSuccess"] = "Template praktek berhasil dihapus.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["PracticalError"] = ex.Message;
        }

        return RedirectToAction(nameof(PracticalTemplates));
    }

    [HttpGet]
    public async Task<IActionResult> PracticalSessions(DateTime? date, CancellationToken cancellationToken)
    {
        var scope = BuildScope();
        var selectedDate = date?.Date ?? DateTime.Today;
        var theoryResults = await _adminService.SearchResultsByNrpAsync(scope, null, cancellationToken);

        var vm = new PracticalSessionManageViewModel
        {
            ExistingCompanies = await _adminService.GetCompaniesAsync(scope, cancellationToken),
            ExistingEmployees = await _adminService.GetEmployeesAsync(scope, cancellationToken),
            ExistingVehicles = await _adminService.GetVehiclesAsync(scope, cancellationToken),
            ExistingInstructors = await _adminService.GetInstructorsAsync(scope, cancellationToken),
            ExistingTemplates = await _adminService.GetPracticalTemplatesAsync(scope, cancellationToken),
            ExistingSessions = (await _adminService.GetPracticalSessionsAsync(scope, cancellationToken))
                .Where(x => x.ScheduledAt.ToLocalTime().Date == selectedDate)
                .ToList(),
            IsAdministrator = scope.IsAdministrator,
            ScheduledAt = selectedDate.AddHours(8),
            Date = selectedDate,
            TheoryCompletedKeys = theoryResults
                .Select(result => $"{result.Nrp}|{result.VehicleId}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PracticalSessions(PracticalSessionManageViewModel model, CancellationToken cancellationToken)
    {
        var scope = BuildScope();
        model.ExistingCompanies = await _adminService.GetCompaniesAsync(scope, cancellationToken);
        model.ExistingEmployees = await _adminService.GetEmployeesAsync(scope, cancellationToken);
        model.ExistingVehicles = await _adminService.GetVehiclesAsync(scope, cancellationToken);
        model.ExistingInstructors = await _adminService.GetInstructorsAsync(scope, cancellationToken);
        model.ExistingTemplates = await _adminService.GetPracticalTemplatesAsync(scope, cancellationToken);
        model.ExistingSessions = (await _adminService.GetPracticalSessionsAsync(scope, cancellationToken))
            .Where(x => x.ScheduledAt.ToLocalTime().Date == model.Date.Date)
            .ToList();
        model.IsAdministrator = scope.IsAdministrator;
        var theoryResults = await _adminService.SearchResultsByNrpAsync(scope, null, cancellationToken);
        model.TheoryCompletedKeys = theoryResults
            .Select(result => $"{result.Nrp}|{result.VehicleId}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var request = new PracticalSessionCreateDto
            {
                EmployeeId = model.EmployeeId,
                VehicleId = model.VehicleId,
                TemplateId = model.TemplateId,
                InstructorUserId = model.InstructorUserId,
                ScheduledAt = model.ScheduledAt
            };

            if (long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                request.CreatedByUserId = userId;
            }

            await _adminService.CreatePracticalSessionAsync(scope, request, cancellationToken);
            TempData["PracticalSuccess"] = "Jadwal praktek berhasil dikirim ke instruktur.";
            return RedirectToAction(nameof(PracticalSessions), new { date = model.Date.Date });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MovePracticalSession([FromBody] PracticalSessionMoveDto request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Request tidak valid." });
        }

        var result = await _adminService.MovePracticalSessionAsync(BuildScope(), request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RemovePracticalSession([FromBody] PracticalSessionRemoveDto request, CancellationToken cancellationToken)
    {
        var result = await _adminService.RemovePracticalSessionAsync(BuildScope(), request.SessionId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GeneratePracticalLink([FromBody] PracticalSessionRemoveDto request, CancellationToken cancellationToken)
    {
        if (request is null || request.SessionId <= 0)
        {
            return BadRequest(new { message = "Data sesi praktek tidak valid." });
        }

        var session = await _adminService.GetPracticalEvaluationAsync(
            BuildScope(),
            request.SessionId,
            GetCurrentUserId(),
            false,
            cancellationToken);

        if (session is null)
        {
            return BadRequest(new { message = "Sesi praktek tidak ditemukan pada scope login." });
        }

        var link = Url.Action("Evaluate", "Practical", new { id = request.SessionId }, Request.Scheme)
            ?? $"/Practical/Evaluate/{request.SessionId}";

        return Ok(new
        {
            link,
            employee = session.EmployeeName,
            nrp = session.Nrp,
            vehicle = session.VehicleName,
            instructor = session.InstructorName,
            scheduleAt = session.ScheduledAt
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AssignSchedule([FromBody] ScheduleAssignRequestDto request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Request tidak valid." });
        }

        if (long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            request.CreatedByUserId = userId;
        }

        (bool Success, string Message, long? ScheduleId) result;
        try
        {
            result = await _adminService.AssignScheduleAsync(BuildScope(), request, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MoveSchedule([FromBody] ScheduleMoveRequestDto request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Request tidak valid." });
        }

        var result = await _adminService.MoveScheduleAsync(BuildScope(), request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RemoveSchedule([FromBody] ScheduleRemoveRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _adminService.RemoveScheduleAsync(BuildScope(), request.ScheduleId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GenerateAccessFromSchedule([FromBody] ScheduleGenerateAccessRequestDto request, CancellationToken cancellationToken)
    {
        if (request is null || request.EmployeeId <= 0 || request.VehicleId <= 0)
        {
            return BadRequest(new { message = "Data generate akses tidak valid." });
        }

        var scope = BuildScope();
        var employees = await _adminService.GetEmployeesAsync(scope, cancellationToken);
        var vehicles = await _adminService.GetVehiclesAsync(scope, cancellationToken);

        var employee = employees.FirstOrDefault(x => x.Id == request.EmployeeId);
        var vehicle = vehicles.FirstOrDefault(x => x.Id == request.VehicleId);
        if (employee is null || vehicle is null)
        {
            return BadRequest(new { message = "Peserta atau vehicle tidak ditemukan pada scope user login." });
        }

        if (scope.HasCompanyScope && employee.CompanyId != vehicle.CompanyId)
        {
            return BadRequest(new { message = "Peserta dan vehicle harus berasal dari company yang sama." });
        }

        string token;
        string refId;

        var activeMonitors = await _adminService.GetActiveExamMonitorsAsync(scope, cancellationToken);
        var existing = activeMonitors.FirstOrDefault(x => x.Nrp == employee.Nrp && x.VehicleId == request.VehicleId);
        if (existing is not null)
        {
            token = existing.Token;
            refId = existing.RefId;
        }
        else
        {
            try
            {
                var session = await _examService.CreateSessionAsync(
                    new SessionCreateRequestDto(employee.Nrp, request.VehicleId),
                    cancellationToken);
                token = session.Token;
                refId = session.RefId;
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        var link = Url.Action("Access", "Exam", new { refId }, Request.Scheme) ?? $"/Exam/Access?refId={refId}";
        return Ok(new
        {
            link,
            refId,
            token,
            employee = employee.EmployeeName,
            nrp = employee.Nrp,
            vehicle = vehicle.VehicleName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Questions(QuestionManageViewModel model, CancellationToken cancellationToken)
    {
        if (model.ImageFile is not null && model.VideoFile is not null)
        {
            ModelState.AddModelError(string.Empty, "Upload hanya salah satu media: gambar atau video.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateQuestionViewModelAsync(model, BuildScope(), cancellationToken);
            return View(model);
        }

        var scope = BuildScope();
        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            model.CompanyId = scope.CompanyId!.Value;
        }

        string? imageUrl;
        string? videoUrl;
        try
        {
            imageUrl = model.ImageFile is null
                ? null
                : await SaveQuestionMediaAsync(model.ImageFile, "images", cancellationToken);

            videoUrl = model.VideoFile is null
                ? null
                : await SaveQuestionMediaAsync(model.VideoFile, "videos", cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateQuestionViewModelAsync(model, scope, cancellationToken);
            return View(model);
        }

        var question = new Question
        {
            CompanyId = model.CompanyId,
            VehicleId = model.VehicleId,
            QuestionText = model.QuestionText,
            OptionA = model.OptionA,
            OptionB = model.OptionB,
            OptionC = model.OptionC,
            OptionD = model.OptionD,
            CorrectAnswer = model.CorrectAnswer,
            Difficulty = model.Difficulty,
            ImageUrl = imageUrl,
            VideoUrl = videoUrl
        };

        await _adminService.AddQuestionAsync(question, cancellationToken);
        TempData["QuestionSuccess"] = "Soal baru berhasil disimpan.";
        return RedirectToAction(nameof(Questions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuestion(
        [FromForm] long questionId,
        [FromForm] long companyId,
        [FromForm] long vehicleId,
        [FromForm] string questionText,
        [FromForm] string optionA,
        [FromForm] string optionB,
        [FromForm] string optionC,
        [FromForm] string optionD,
        [FromForm] string correctAnswer,
        [FromForm] string difficulty,
        [FromForm] bool removeImage,
        [FromForm] bool removeVideo,
        [FromForm] IFormFile? imageFile,
        [FromForm] IFormFile? videoFile,
        CancellationToken cancellationToken)
    {
        var scope = BuildScope();
        var allowedQuestionIds = await _adminService.GetQuestionsAsync(scope, cancellationToken);
        if (!allowedQuestionIds.Any(x => x.Id == questionId))
        {
            TempData["QuestionError"] = "Soal tidak ditemukan atau tidak bisa diakses.";
            return RedirectToAction(nameof(Questions));
        }

        var question = await _adminService.GetQuestionByIdAsync(questionId, cancellationToken);
        if (question is null)
        {
            TempData["QuestionError"] = "Soal tidak ditemukan.";
            return RedirectToAction(nameof(Questions));
        }

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            companyId = scope.CompanyId!.Value;
        }

        if (imageFile is not null && videoFile is not null)
        {
            TempData["QuestionError"] = "Upload hanya salah satu media: gambar atau video.";
            return RedirectToAction(nameof(Questions));
        }

        if (string.IsNullOrWhiteSpace(questionText) ||
            string.IsNullOrWhiteSpace(optionA) ||
            string.IsNullOrWhiteSpace(optionB) ||
            string.IsNullOrWhiteSpace(optionC) ||
            string.IsNullOrWhiteSpace(optionD))
        {
            TempData["QuestionError"] = "Pertanyaan dan semua opsi jawaban wajib diisi.";
            return RedirectToAction(nameof(Questions));
        }

        correctAnswer = correctAnswer.Trim().ToUpperInvariant();
        if (correctAnswer is not ("A" or "B" or "C" or "D"))
        {
            TempData["QuestionError"] = "Kunci jawaban harus A, B, C, atau D.";
            return RedirectToAction(nameof(Questions));
        }

        difficulty = string.IsNullOrWhiteSpace(difficulty) ? "medium" : difficulty.Trim().ToLowerInvariant();
        if (difficulty is not ("easy" or "medium" or "hard"))
        {
            TempData["QuestionError"] = "Level soal harus easy, medium, atau hard.";
            return RedirectToAction(nameof(Questions));
        }

        try
        {
            if (removeImage && !string.IsNullOrWhiteSpace(question.ImageUrl))
            {
                DeleteQuestionMediaFile(question.ImageUrl);
                question.ImageUrl = null;
            }

            if (removeVideo && !string.IsNullOrWhiteSpace(question.VideoUrl))
            {
                DeleteQuestionMediaFile(question.VideoUrl);
                question.VideoUrl = null;
            }

            if (imageFile is not null)
            {
                if (!string.IsNullOrWhiteSpace(question.ImageUrl))
                {
                    DeleteQuestionMediaFile(question.ImageUrl);
                }

                if (!string.IsNullOrWhiteSpace(question.VideoUrl))
                {
                    DeleteQuestionMediaFile(question.VideoUrl);
                    question.VideoUrl = null;
                }

                question.ImageUrl = await SaveQuestionMediaAsync(imageFile, "images", cancellationToken);
            }

            if (videoFile is not null)
            {
                if (!string.IsNullOrWhiteSpace(question.VideoUrl))
                {
                    DeleteQuestionMediaFile(question.VideoUrl);
                }

                if (!string.IsNullOrWhiteSpace(question.ImageUrl))
                {
                    DeleteQuestionMediaFile(question.ImageUrl);
                    question.ImageUrl = null;
                }

                question.VideoUrl = await SaveQuestionMediaAsync(videoFile, "videos", cancellationToken);
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["QuestionError"] = ex.Message;
            return RedirectToAction(nameof(Questions));
        }

        question.CompanyId = companyId;
        question.VehicleId = vehicleId;
        question.QuestionText = questionText.Trim();
        question.OptionA = optionA.Trim();
        question.OptionB = optionB.Trim();
        question.OptionC = optionC.Trim();
        question.OptionD = optionD.Trim();
        question.CorrectAnswer = correctAnswer;
        question.Difficulty = difficulty;

        await _adminService.UpdateQuestionAsync(question, cancellationToken);
        TempData["QuestionSuccess"] = "Soal berhasil diperbarui.";
        return RedirectToAction(nameof(Questions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion([FromForm] long questionId, CancellationToken cancellationToken)
    {
        var scope = BuildScope();
        var allowedQuestions = await _adminService.GetQuestionsAsync(scope, cancellationToken);
        if (!allowedQuestions.Any(x => x.Id == questionId))
        {
            TempData["QuestionError"] = "Soal tidak ditemukan atau tidak bisa dihapus.";
            return RedirectToAction(nameof(Questions));
        }

        var question = await _adminService.GetQuestionByIdAsync(questionId, cancellationToken);
        if (question is null)
        {
            TempData["QuestionError"] = "Soal tidak ditemukan.";
            return RedirectToAction(nameof(Questions));
        }

        if (!string.IsNullOrWhiteSpace(question.ImageUrl))
        {
            DeleteQuestionMediaFile(question.ImageUrl);
        }

        if (!string.IsNullOrWhiteSpace(question.VideoUrl))
        {
            DeleteQuestionMediaFile(question.VideoUrl);
        }

        await _adminService.DeleteQuestionAsync(question, cancellationToken);
        TempData["QuestionSuccess"] = "Soal berhasil dihapus.";
        return RedirectToAction(nameof(Questions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportQuestionsExcel(QuestionManageViewModel model, CancellationToken cancellationToken)
    {
        var scope = BuildScope();
        await PopulateQuestionViewModelAsync(model, scope, cancellationToken);

        if (model.BulkExcelFile is null || model.BulkExcelFile.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "File Excel wajib dipilih.");
            return View("Questions", model);
        }

        var ext = Path.GetExtension(model.BulkExcelFile.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            ModelState.AddModelError(string.Empty, "Format file harus .xlsx");
            return View("Questions", model);
        }

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            model.ImportCompanyId = scope.CompanyId;
        }

        var companyByName = model.ExistingCompanies
            .GroupBy(x => x.CompanyName.Trim().ToUpperInvariant())
            .ToDictionary(x => x.Key, x => x.First());
        var vehicles = model.ExistingVehicles;
        var existingKeys = model.ExistingQuestions
            .Select(x => $"{x.CompanyId}|{x.VehicleId}|{NormalizeText(x.QuestionText)}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var batchKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var toSave = new List<Question>();

        using var stream = model.BulkExcelFile.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.FirstOrDefault(x => x.Name.Equals("Questions", StringComparison.OrdinalIgnoreCase))
                 ?? workbook.Worksheets.FirstOrDefault();
        if (ws is null || ws.LastRowUsed() is null)
        {
            ModelState.AddModelError(string.Empty, "Sheet Questions tidak ditemukan atau file kosong.");
            return View("Questions", model);
        }

        var rowIndex = 1;
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            rowIndex++;
            var companyName = row.Cell(1).GetValue<string>().Trim();
            var vehicleName = row.Cell(2).GetValue<string>().Trim();
            var questionText = row.Cell(3).GetValue<string>().Trim();
            var optionA = row.Cell(4).GetValue<string>().Trim();
            var optionB = row.Cell(5).GetValue<string>().Trim();
            var optionC = row.Cell(6).GetValue<string>().Trim();
            var optionD = row.Cell(7).GetValue<string>().Trim();
            var correct = row.Cell(8).GetValue<string>().Trim().ToUpperInvariant();
            var difficulty = row.Cell(9).GetValue<string>().Trim().ToLowerInvariant();
            var imageUrl = row.Cell(10).GetValue<string>().Trim();
            var videoUrl = row.Cell(11).GetValue<string>().Trim();

            if (string.IsNullOrWhiteSpace(questionText) &&
                string.IsNullOrWhiteSpace(optionA) &&
                string.IsNullOrWhiteSpace(optionB) &&
                string.IsNullOrWhiteSpace(optionC) &&
                string.IsNullOrWhiteSpace(optionD))
            {
                continue;
            }

            model.ImportTotalCount++;

            var report = new QuestionImportRowViewModel
            {
                RowNumber = row.RowNumber(),
                CompanyName = companyName,
                VehicleName = vehicleName,
                QuestionText = questionText
            };

            var errors = new List<string>();

            Company? company = null;
            if (model.ImportCompanyId.HasValue && model.ImportCompanyId.Value > 0)
            {
                company = model.ExistingCompanies.FirstOrDefault(x => x.Id == model.ImportCompanyId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(companyName))
            {
                companyByName.TryGetValue(companyName.ToUpperInvariant(), out company);
            }
            else
            {
                errors.Add("Company kosong.");
            }

            if (company is null)
            {
                errors.Add("Company tidak ditemukan pada scope login.");
            }

            Vehicle? vehicle = null;
            if (model.ImportVehicleId.HasValue && model.ImportVehicleId.Value > 0)
            {
                vehicle = vehicles.FirstOrDefault(x => x.Id == model.ImportVehicleId.Value);
            }
            else if (company is not null && !string.IsNullOrWhiteSpace(vehicleName))
            {
                vehicle = vehicles.FirstOrDefault(x =>
                    x.CompanyId == company.Id &&
                    x.VehicleName.Equals(vehicleName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                errors.Add("Vehicle kosong.");
            }

            if (vehicle is null)
            {
                errors.Add("Vehicle tidak ditemukan pada scope login.");
            }
            else if (company is not null && vehicle.CompanyId != company.Id)
            {
                errors.Add("Vehicle tidak sesuai dengan company.");
            }

            if (string.IsNullOrWhiteSpace(questionText))
            {
                errors.Add("QuestionText wajib diisi.");
            }

            if (string.IsNullOrWhiteSpace(optionA) || string.IsNullOrWhiteSpace(optionB) || string.IsNullOrWhiteSpace(optionC) || string.IsNullOrWhiteSpace(optionD))
            {
                errors.Add("Semua opsi A-D wajib diisi.");
            }

            if (correct is not ("A" or "B" or "C" or "D"))
            {
                errors.Add("CorrectAnswer harus A/B/C/D.");
            }

            if (string.IsNullOrWhiteSpace(difficulty))
            {
                difficulty = "medium";
            }

            if (difficulty is not ("easy" or "medium" or "hard"))
            {
                errors.Add("Difficulty harus easy/medium/hard.");
            }

            if (company is not null && vehicle is not null && !string.IsNullOrWhiteSpace(questionText))
            {
                var key = $"{company.Id}|{vehicle.Id}|{NormalizeText(questionText)}";
                if (existingKeys.Contains(key) || batchKeys.Contains(key))
                {
                    errors.Add("Soal duplikat pada company + vehicle yang sama.");
                }
                else
                {
                    batchKeys.Add(key);
                }
            }

            if (errors.Count > 0 || company is null || vehicle is null)
            {
                report.IsSuccess = false;
                report.Message = string.Join(" ", errors);
                model.ImportRows.Add(report);
                model.ImportFailedCount++;
                continue;
            }

            toSave.Add(new Question
            {
                CompanyId = company.Id,
                VehicleId = vehicle.Id,
                QuestionText = questionText,
                OptionA = optionA,
                OptionB = optionB,
                OptionC = optionC,
                OptionD = optionD,
                CorrectAnswer = correct,
                Difficulty = difficulty,
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl,
                VideoUrl = string.IsNullOrWhiteSpace(videoUrl) ? null : videoUrl
            });

            report.IsSuccess = true;
            report.Message = "Valid";
            model.ImportRows.Add(report);
            model.ImportSuccessCount++;
        }

        foreach (var question in toSave)
        {
            await _adminService.AddQuestionAsync(question, cancellationToken);
        }

        model.ImportSummaryMessage = $"Import selesai. Berhasil: {model.ImportSuccessCount}, Gagal: {model.ImportFailedCount}, Total dibaca: {model.ImportTotalCount}.";
        model.ExistingQuestions = await _adminService.GetQuestionsAsync(scope, cancellationToken);
        return View("Questions", model);
    }

    [HttpGet]
    [Authorize(Roles = SystemUserRole.Administrator)]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        var vm = new UserManageViewModel
        {
            ExistingUsers = await _adminService.GetUsersAsync(BuildScope(), cancellationToken),
            ExistingCompanies = await _adminService.GetCompaniesAsync(BuildScope(), cancellationToken),
            Role = SystemUserRole.CompanyAdmin
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = SystemUserRole.Administrator)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Users(UserManageViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.ExistingUsers = await _adminService.GetUsersAsync(BuildScope(), cancellationToken);
            model.ExistingCompanies = await _adminService.GetCompaniesAsync(BuildScope(), cancellationToken);
            return View(model);
        }

        try
        {
            await _adminService.AddUserAsync(new CreateUserDto
            {
                Username = model.Username,
                Password = model.Password,
                FullName = model.FullName,
                Role = model.Role,
                CompanyId = model.CompanyId
            }, cancellationToken);

            return RedirectToAction(nameof(Users));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.ExistingUsers = await _adminService.GetUsersAsync(BuildScope(), cancellationToken);
            model.ExistingCompanies = await _adminService.GetCompaniesAsync(BuildScope(), cancellationToken);
            return View(model);
        }
    }

    [HttpPost]
    [Authorize(Roles = SystemUserRole.Administrator)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUser(UserEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["UserError"] = "Data update user tidak valid.";
            return RedirectToAction(nameof(Users));
        }

        try
        {
            await _adminService.UpdateUserAsync(new UpdateUserDto
            {
                UserId = model.UserId,
                FullName = model.FullName,
                Role = model.Role,
                CompanyId = model.CompanyId
            }, cancellationToken);

            TempData["UserSuccess"] = "User berhasil diperbarui.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["UserError"] = ex.Message;
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [Authorize(Roles = SystemUserRole.Administrator)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetUserPassword(UserResetPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (model.UserId <= 0)
        {
            TempData["UserError"] = "User tidak valid.";
            return RedirectToAction(nameof(Users));
        }

        var newPassword = string.IsNullOrWhiteSpace(model.NewPassword)
            ? GenerateSystemPassword(10)
            : model.NewPassword.Trim();

        if (newPassword.Length < 6)
        {
            TempData["UserError"] = "Password baru minimal 6 karakter.";
            return RedirectToAction(nameof(Users));
        }

        try
        {
            await _adminService.ResetUserPasswordAsync(new ResetUserPasswordDto
            {
                UserId = model.UserId,
                NewPassword = newPassword
            }, GetCurrentUserId(), cancellationToken);

            TempData["UserSuccess"] = "Password user berhasil direset.";
            TempData["UserResetNewPassword"] = newPassword;
        }
        catch (InvalidOperationException ex)
        {
            TempData["UserError"] = ex.Message;
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [Authorize(Roles = SystemUserRole.Administrator)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(long userId, CancellationToken cancellationToken)
    {
        try
        {
            await _adminService.DeleteUserAsync(userId, GetCurrentUserId(), cancellationToken);
            TempData["UserSuccess"] = "User berhasil dihapus dari akses login.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["UserError"] = ex.Message;
        }

        return RedirectToAction(nameof(Users));
    }

    private AccessScopeDto BuildScope()
    {
        var isAdmin = User.IsInRole(SystemUserRole.Administrator);
        if (isAdmin)
        {
            return new AccessScopeDto(true, null);
        }

        var companyClaim = User.FindFirstValue("company_id");
        if (long.TryParse(companyClaim, out var companyId) && companyId > 0)
        {
            return new AccessScopeDto(false, companyId);
        }

        return new AccessScopeDto(false, null);
    }

    private long? GetCurrentUserId()
    {
        return long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
    }

    private static string SanitizeFileToken(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var sanitized = new string(value
            .Trim()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray())
            .Trim('-');

        return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
    }

    private async Task<SummaryScoreViewModel?> BuildSummaryScoreViewModelAsync(long theorySessionId, long practicalSessionId, CancellationToken cancellationToken)
    {
        var summary = await _adminService.GetSummaryScoreAsync(
            BuildScope(),
            theorySessionId,
            practicalSessionId,
            GetCurrentUserId(),
            false,
            cancellationToken);

        if (summary is null)
        {
            return null;
        }

        var companyLogoPath = Path.Combine(_environment.WebRootPath, "images", "logo indexim.png");
        var appLogoPath = Path.Combine(_environment.WebRootPath, "logo", "logo.png");
        var qrProofText = $"TEST-ID {summary.SummaryId}";
        var qrTargetUrl = BuildPublicSummaryUrl(theorySessionId, practicalSessionId);

        return new SummaryScoreViewModel
        {
            IsComplete = summary.IsComplete,
            SummaryId = summary.SummaryId,
            CompanyName = string.IsNullOrWhiteSpace(summary.CompanyName) ? "PT INDEXIM COALINDO" : summary.CompanyName,
            EmployeeName = summary.EmployeeName,
            Nrp = summary.Nrp,
            Ktp = summary.Ktp,
            SubmissionNumber = summary.SubmissionNumber,
            SubmissionType = summary.SubmissionType,
            DepartmentName = summary.DepartmentName,
            VehicleName = summary.VehicleName,
            InstructorName = summary.InstructorName,
            TheoryScheduledAt = summary.TheoryScheduledAt,
            TheoryFinishedAt = summary.TheoryFinishedAt,
            PracticalScheduledAt = summary.PracticalScheduledAt,
            PracticalFinishedAt = summary.PracticalFinishedAt,
            TheoryScore = summary.TheoryScore,
            TheoryPassed = summary.TheoryPassed,
            TheoryTotalQuestions = summary.TheoryTotalQuestions,
            TheoryCorrectAnswers = summary.TheoryCorrectAnswers,
            PracticalNumericScore = summary.PracticalNumericScore,
            PracticalGrade = summary.PracticalGrade,
            PracticalPassed = summary.PracticalPassed,
            PracticalTemplateName = summary.PracticalTemplateName,
            PracticalScoringMode = summary.PracticalScoringMode,
            InstructorNote = summary.InstructorNote,
            TheorySessionId = summary.TheorySessionId,
            PracticalSessionId = summary.PracticalSessionId,
            QrProofText = qrProofText,
            QrTargetUrl = qrTargetUrl,
            QrCodeDataUrl = BuildQrCodeDataUrl(qrTargetUrl),
            CompanyLogoPath = companyLogoPath,
            AppLogoPath = appLogoPath,
            PracticalItems = summary.PracticalItems
        };
    }

    private string BuildPublicSummaryUrl(long theorySessionId, long practicalSessionId)
    {
        return Url.Action("PublicSummary", "Exam", new { theorySessionId, practicalSessionId }, Request.Scheme)
            ?? $"{Request.Scheme}://{Request.Host}/Exam/PublicSummary?theorySessionId={theorySessionId}&practicalSessionId={practicalSessionId}";
    }

    private static string BuildQrCodeDataUrl(string text)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrData);
        var bytes = pngQr.GetGraphic(10);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }

    private async Task<string> SaveQuestionMediaAsync(IFormFile file, string typeFolder, CancellationToken cancellationToken)
    {
        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "questions", typeFolder);
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var allowedVideoExtensions = new[] { ".mp4", ".webm", ".ogg" };

        if (typeFolder == "images" && !allowedImageExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Format gambar tidak didukung. Gunakan jpg, jpeg, png, atau webp.");
        }

        if (typeFolder == "videos" && !allowedVideoExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Format video tidak didukung. Gunakan mp4, webm, atau ogg.");
        }

        var uniqueName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, uniqueName);

        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/questions/{typeFolder}/{uniqueName}";
    }

    private void DeleteQuestionMediaFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        var trimmed = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_environment.WebRootPath, trimmed);
        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }

    private async Task PopulateQuestionViewModelAsync(QuestionManageViewModel model, AccessScopeDto scope, CancellationToken cancellationToken)
    {
        model.ExistingQuestions = await _adminService.GetQuestionsAsync(scope, cancellationToken);
        model.ExistingCompanies = await _adminService.GetCompaniesAsync(scope, cancellationToken);
        model.ExistingVehicles = await _adminService.GetVehiclesAsync(scope, cancellationToken);

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            model.CompanyId = scope.CompanyId!.Value;
            model.ImportCompanyId = scope.CompanyId;
        }
    }

    private static string NormalizeText(string value)
    {
        return string.Join(" ", value
            .Trim()
            .ToUpperInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static List<PracticalTemplateItemInputDto> ParsePracticalTemplateItems(string rawValue)
    {
        var items = new List<PracticalTemplateItemInputDto>();
        var lines = rawValue
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            {
                continue;
            }

            var sectionName = parts.Length >= 2 ? parts[0] : "General";
            var itemLabel = parts.Length >= 2 ? parts[1] : parts[0];
            var weight = 1m;

            if (parts.Length >= 3 && decimal.TryParse(parts[2], out var parsedWeight) && parsedWeight > 0)
            {
                weight = parsedWeight;
            }

            items.Add(new PracticalTemplateItemInputDto
            {
                SectionName = sectionName,
                ItemLabel = itemLabel,
                Weight = weight
            });
        }

        return items;
    }

    private static string GenerateSystemPassword(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = new byte[length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        var result = new char[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }

        return new string(result);
    }
}
