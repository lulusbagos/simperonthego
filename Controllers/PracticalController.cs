using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimperSecureOnlineTestSystem.Application.DTOs;
using SimperSecureOnlineTestSystem.Application.Services;
using SimperSecureOnlineTestSystem.Domain.Enums;
using SimperSecureOnlineTestSystem.ViewModels;

namespace SimperSecureOnlineTestSystem.Controllers;

[Authorize(Roles = SystemUserRole.Administrator + "," + SystemUserRole.CompanyAdmin + "," + SystemUserRole.Instructor)]
public class PracticalController : Controller
{
    private readonly IAdminService _adminService;

    public PracticalController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> MyAssignments(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var vm = new PracticalMyAssignmentsViewModel
        {
            UserRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
            Sessions = User.IsInRole(SystemUserRole.Instructor)
                ? await _adminService.GetInstructorPracticalSessionsAsync(userId, cancellationToken)
                : await _adminService.GetPracticalSessionsAsync(BuildScope(), cancellationToken)
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Evaluate(long id, CancellationToken cancellationToken)
    {
        var evaluation = await _adminService.GetPracticalEvaluationAsync(
            BuildScope(),
            id,
            GetCurrentUserId(),
            User.IsInRole(SystemUserRole.Instructor),
            cancellationToken);

        if (evaluation is null)
        {
            return NotFound();
        }

        return View(MapEvaluationViewModel(evaluation));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Evaluate(PracticalEvaluationInputViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            await _adminService.SubmitPracticalEvaluationAsync(
                BuildScope(),
                new PracticalEvaluationSubmitDto
                {
                    SessionId = model.SessionId,
                    InstructorNote = model.InstructorNote,
                    Items = model.Items.Select(x => new PracticalEvaluationItemSubmitDto
                    {
                        TemplateItemId = x.TemplateItemId,
                        NumericValue = x.NumericValue,
                        GradeValue = x.GradeValue,
                        Note = x.Note
                    }).ToList()
                },
                GetCurrentUserId(),
                User.IsInRole(SystemUserRole.Instructor),
                cancellationToken);

            TempData["PracticalSuccess"] = "Penilaian praktek berhasil disimpan.";
            return RedirectToAction(nameof(MyAssignments));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var evaluation = await _adminService.GetPracticalEvaluationAsync(
                BuildScope(),
                model.SessionId,
                GetCurrentUserId(),
                User.IsInRole(SystemUserRole.Instructor),
                cancellationToken);

            if (evaluation is null)
            {
                return NotFound();
            }

            var vm = MapEvaluationViewModel(evaluation);
            for (var i = 0; i < Math.Min(vm.Items.Count, model.Items.Count); i++)
            {
                vm.Items[i].NumericValue = model.Items[i].NumericValue;
                vm.Items[i].GradeValue = model.Items[i].GradeValue;
                vm.Items[i].Note = model.Items[i].Note;
            }
            vm.InstructorNote = model.InstructorNote;

            return View(vm);
        }
    }

    private AccessScopeDto BuildScope()
    {
        if (User.IsInRole(SystemUserRole.Administrator))
        {
            return new AccessScopeDto(true, null);
        }

        var companyClaim = User.FindFirstValue("company_id");
        return long.TryParse(companyClaim, out var companyId) && companyId > 0
            ? new AccessScopeDto(false, companyId)
            : new AccessScopeDto(false, null);
    }

    private long? GetCurrentUserId()
    {
        return TryGetCurrentUserId(out var userId) ? userId : null;
    }

    private bool TryGetCurrentUserId(out long userId)
    {
        userId = 0;
        return long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }

    private static PracticalEvaluationInputViewModel MapEvaluationViewModel(PracticalEvaluationDto evaluation)
    {
        return new PracticalEvaluationInputViewModel
        {
            SessionId = evaluation.SessionId,
            EmployeeName = evaluation.EmployeeName,
            Nrp = evaluation.Nrp,
            Ktp = evaluation.Ktp,
            SubmissionNumber = evaluation.SubmissionNumber,
            SubmissionType = evaluation.SubmissionType,
            DepartmentName = evaluation.DepartmentName,
            CompanyName = evaluation.CompanyName,
            VehicleName = evaluation.VehicleName,
            TemplateName = evaluation.TemplateName,
            ScoringMode = evaluation.ScoringMode,
            InstructorName = evaluation.InstructorName,
            ScheduledAt = evaluation.ScheduledAt,
            PassingScore = evaluation.PassingScore,
            PassingGrade = evaluation.PassingGrade,
            GradeOptions = evaluation.GradeOptions ?? string.Empty,
            FinalNumericScore = evaluation.FinalNumericScore,
            FinalGrade = evaluation.FinalGrade,
            PassStatus = evaluation.PassStatus,
            ExistingInstructorNote = evaluation.InstructorNote,
            InstructorNote = evaluation.InstructorNote,
            Items = evaluation.Items.Select(x => new PracticalEvaluationItemInputViewModel
            {
                TemplateItemId = x.TemplateItemId,
                SectionName = x.SectionName,
                ItemLabel = x.ItemLabel,
                Weight = x.Weight,
                NumericValue = x.NumericValue,
                GradeValue = x.GradeValue,
                Note = x.Note
            }).ToList()
        };
    }
}
