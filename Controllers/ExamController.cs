using Microsoft.AspNetCore.Mvc;
using QRCoder;
using SimperSecureOnlineTestSystem.Application.DTOs;
using SimperSecureOnlineTestSystem.Application.Services;
using SimperSecureOnlineTestSystem.ViewModels;

namespace SimperSecureOnlineTestSystem.Controllers;

public class ExamController : Controller
{
    private readonly IExamService _examService;
    private readonly IAdminService _adminService;
    private readonly IWebHostEnvironment _environment;

    public ExamController(IExamService examService, IAdminService adminService, IWebHostEnvironment environment)
    {
        _examService = examService;
        _adminService = adminService;
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(EnterRefId));
    }

    [HttpGet]
    public IActionResult EnterRefId()
    {
        return View(new RefIdInputViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoToAccessByRefId(RefIdInputViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("EnterRefId", model);
        }

        var normalizedRefId = model.RefId.Trim().ToUpperInvariant();
        var token = await _examService.ResolveTokenByRefIdAsync(normalizedRefId, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            ModelState.AddModelError(nameof(model.RefId), "Ref ID tidak ditemukan.");
            return View("EnterRefId", model);
        }

        return RedirectToAction(nameof(Start), new { token });
    }

    [HttpGet]
    public IActionResult EnterNrp()
    {
        return View(new NrpInputViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyNrp(NrpInputViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("EnterNrp", model);
        }

        var employee = await _examService.VerifyEmployeeAsync(model.Nrp, cancellationToken);
        if (employee is null)
        {
            ModelState.AddModelError(nameof(model.Nrp), "NRP not found.");
            return View("EnterNrp", model);
        }

        return RedirectToAction(nameof(SelectExam), new { nrp = employee.Nrp });
    }

    [HttpGet]
    public async Task<IActionResult> SelectExam(string nrp, CancellationToken cancellationToken)
    {
        var employee = await _examService.VerifyEmployeeAsync(nrp, cancellationToken);
        if (employee is null)
        {
            return RedirectToAction(nameof(EnterRefId));
        }

        var vehicles = await _examService.GetVehiclesByNrpAsync(nrp, cancellationToken);
        var vm = new SelectExamViewModel
        {
            Nrp = nrp,
            EmployeeName = employee.EmployeeName,
            Vehicles = vehicles
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSession(SelectExamViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var employee = await _examService.VerifyEmployeeAsync(model.Nrp, cancellationToken);
            model.EmployeeName = employee?.EmployeeName ?? string.Empty;
            model.Vehicles = await _examService.GetVehiclesByNrpAsync(model.Nrp, cancellationToken);
            return View("SelectExam", model);
        }

        try
        {
            var session = await _examService.CreateSessionAsync(new SessionCreateRequestDto(model.Nrp, model.VehicleId), cancellationToken);
            var link = Url.Action(nameof(Access), "Exam", new { refId = session.RefId }, Request.Scheme) ?? $"/Exam/Access?refId={session.RefId}";

            return View("SessionGenerated", new SessionGeneratedViewModel
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
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.Vehicles = await _examService.GetVehiclesByNrpAsync(model.Nrp, cancellationToken);
            return View("SelectExam", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Access(string? token, string? refId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(refId))
        {
            token = await _examService.ResolveTokenByRefIdAsync(refId.Trim().ToUpperInvariant(), cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(nameof(EnterRefId));
        }

        var validation = await _examService.ValidateTokenAsync(token, cancellationToken);
        if (!validation.IsValid)
        {
            TempData["RefIdError"] = validation.Message;
            return RedirectToAction(nameof(EnterRefId));
        }

        return RedirectToAction(nameof(Start), new { token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Access(ExamAccessViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var tokenValidation = await _examService.ValidateTokenAsync(model.Token, cancellationToken);
        if (!tokenValidation.IsValid)
        {
            ModelState.AddModelError(string.Empty, tokenValidation.Message);
            return View(model);
        }

        return RedirectToAction(nameof(Start), new { token = model.Token });
    }

    [HttpGet]
    public async Task<IActionResult> Start(string token, CancellationToken cancellationToken)
    {
        var validation = await _examService.ValidateTokenAsync(token, cancellationToken);
        if (!validation.IsValid)
        {
            TempData["RefIdError"] = validation.Message;
            return RedirectToAction(nameof(EnterRefId));
        }

        var hasQuestions = await _examService.SessionHasQuestionsAsync(token, cancellationToken);
        if (!hasQuestions)
        {
            TempData["RefIdError"] = "Sesi ujian ini belum memiliki soal. Minta admin membuat ulang Ref ID setelah bank soal dilengkapi.";
            return RedirectToAction(nameof(EnterRefId));
        }

        await _examService.StartSessionAsync(token, cancellationToken);

        return View(new ExamStartViewModel
        {
            Token = token,
            EndTime = validation.EndTime
        });
    }

    [HttpGet]
    public async Task<IActionResult> PublicSummary(long theorySessionId, long practicalSessionId, CancellationToken cancellationToken)
    {
        var summary = await _adminService.GetSummaryScoreAsync(
            new AccessScopeDto(true, null),
            theorySessionId,
            practicalSessionId,
            null,
            false,
            cancellationToken);

        if (summary is null || !summary.IsComplete)
        {
            return NotFound();
        }

        var qrProofText = $"TEST-ID {summary.SummaryId}";
        var qrTargetUrl = Url.Action(nameof(PublicSummary), "Exam", new { theorySessionId, practicalSessionId }, Request.Scheme)
            ?? $"{Request.Scheme}://{Request.Host}/Exam/PublicSummary?theorySessionId={theorySessionId}&practicalSessionId={practicalSessionId}";

        return View("../Admin/SummaryScore", new SummaryScoreViewModel
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
            CompanyLogoPath = Path.Combine(_environment.WebRootPath, "images", "logo indexim.png"),
            AppLogoPath = Path.Combine(_environment.WebRootPath, "logo", "logo.png"),
            PracticalItems = summary.PracticalItems
        });
    }

    [HttpGet]
    public async Task<IActionResult> Question(string token, int order = 1, CancellationToken cancellationToken = default)
    {
        var question = await _examService.GetQuestionAsync(token, order, cancellationToken);
        if (question is null)
        {
            return NotFound();
        }

        return Json(question);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveAnswer([FromBody] SaveAnswerRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _examService.SaveAnswerAsync(request, cancellationToken);
            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> LogEvent([FromBody] LogEventDto request, CancellationToken cancellationToken)
    {
        await _examService.LogEventAsync(request, cancellationToken);
        return Ok();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CameraStatus([FromBody] CameraStatusDto request, CancellationToken cancellationToken)
    {
        await _examService.UpdateCameraStatusAsync(request, cancellationToken);
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(string token, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _examService.SubmitSessionAsync(token, cancellationToken);
            return View("Result", new ExamResultViewModel { Token = token, Result = result });
        }
        catch (InvalidOperationException ex)
        {
            var validation = await _examService.ValidateTokenAsync(token, cancellationToken);
            if (!validation.IsValid)
            {
                TempData["RefIdError"] = ex.Message;
                return RedirectToAction(nameof(EnterRefId));
            }

            return View("Start", new ExamStartViewModel
            {
                Token = token,
                EndTime = validation.EndTime,
                ErrorMessage = ex.Message
            });
        }
    }

    private static string BuildQrCodeDataUrl(string text)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrData);
        var bytes = pngQr.GetGraphic(10);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
