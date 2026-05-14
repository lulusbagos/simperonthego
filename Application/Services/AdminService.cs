using System.Text;
using Microsoft.AspNetCore.Identity;
using SimperSecureOnlineTestSystem.Application.DTOs;
using SimperSecureOnlineTestSystem.Domain.Entities;
using SimperSecureOnlineTestSystem.Domain.Enums;
using SimperSecureOnlineTestSystem.Infrastructure.Repositories;

namespace SimperSecureOnlineTestSystem.Application.Services;

public class AdminService : IAdminService
{
    private const int MaxParticipantsPerSession = 5;
    private readonly IAdminRepository _adminRepository;
    private readonly PasswordHasher<UserLogin> _passwordHasher = new();

    public AdminService(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public Task<List<AdminExamMonitorDto>> GetActiveExamMonitorsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        return _adminRepository.GetActiveExamMonitorsAsync(scope, cancellationToken);
    }

    public Task<List<EmployeeResultDto>> SearchResultsByNrpAsync(AccessScopeDto scope, string? nrp, CancellationToken cancellationToken = default)
    {
        return _adminRepository.SearchResultsByNrpAsync(scope, nrp, cancellationToken);
    }

    public async Task<byte[]> ExportResultsCsvAsync(AccessScopeDto scope, string? nrp, CancellationToken cancellationToken = default)
    {
        var results = await _adminRepository.SearchResultsByNrpAsync(scope, nrp, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("NRP,EmployeeName,VehicleName,Score,PassStatus,FinishedAtUtc");

        foreach (var item in results)
        {
            builder.AppendLine($"{Escape(item.Nrp)},{Escape(item.EmployeeName)},{Escape(item.VehicleName)},{item.Score},{item.PassStatus},{item.FinishedAt:O}");
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public Task<List<Employee>> GetEmployeesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        return _adminRepository.GetEmployeesAsync(scope, cancellationToken);
    }

    public Task<int> CountSimperApplicantsAsync(AccessScopeDto scope, string? searchTerm, CancellationToken cancellationToken = default)
    {
        return _adminRepository.CountSimperApplicantsAsync(scope, searchTerm, cancellationToken);
    }

    public Task<List<SimperApplicantView>> GetSimperApplicantsAsync(AccessScopeDto scope, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return _adminRepository.GetSimperApplicantsAsync(scope, searchTerm, page, pageSize, cancellationToken);
    }

    public Task<List<Vehicle>> GetVehiclesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        return _adminRepository.GetVehiclesAsync(scope, cancellationToken);
    }

    public Task<List<Question>> GetQuestionsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        return _adminRepository.GetQuestionsAsync(scope, cancellationToken);
    }

    public Task<Question?> GetQuestionByIdAsync(long questionId, CancellationToken cancellationToken = default)
    {
        return _adminRepository.GetQuestionByIdAsync(questionId, cancellationToken);
    }

    public Task<List<Company>> GetCompaniesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        return _adminRepository.GetCompaniesAsync(scope, cancellationToken);
    }

    public Task AddEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        employee.CreatedAt = DateTime.UtcNow;
        return _adminRepository.AddEmployeeAsync(employee, cancellationToken);
    }

    public Task AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        vehicle.CreatedAt = DateTime.UtcNow;
        return _adminRepository.AddVehicleAsync(vehicle, cancellationToken);
    }

    public Task AddQuestionAsync(Question question, CancellationToken cancellationToken = default)
    {
        question.CreatedAt = DateTime.UtcNow;
        return _adminRepository.AddQuestionAsync(question, cancellationToken);
    }

    public Task UpdateQuestionAsync(Question question, CancellationToken cancellationToken = default)
    {
        return _adminRepository.UpdateQuestionAsync(question, cancellationToken);
    }

    public Task DeleteQuestionAsync(Question question, CancellationToken cancellationToken = default)
    {
        return _adminRepository.DeleteQuestionAsync(question, cancellationToken);
    }

    public async Task AddCompanyAsync(CompanyUpsertDto company, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(company.CompanyName))
        {
            throw new InvalidOperationException("Nama company wajib diisi.");
        }

        var existing = await _adminRepository.GetCompanyByNameAsync(company.CompanyName, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("Nama company sudah ada.");
        }

        await _adminRepository.AddCompanyAsync(new Company
        {
            CompanyName = company.CompanyName.Trim(),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task UpdateCompanyAsync(CompanyUpsertDto company, CancellationToken cancellationToken = default)
    {
        if (company.CompanyId <= 0)
        {
            throw new InvalidOperationException("Company tidak valid.");
        }

        if (string.IsNullOrWhiteSpace(company.CompanyName))
        {
            throw new InvalidOperationException("Nama company wajib diisi.");
        }

        var existing = await _adminRepository.GetCompanyByIdAsync(company.CompanyId, cancellationToken);
        if (existing is null)
        {
            throw new InvalidOperationException("Company tidak ditemukan.");
        }

        var duplicate = await _adminRepository.GetCompanyByNameAsync(company.CompanyName, cancellationToken);
        if (duplicate is not null && duplicate.Id != company.CompanyId)
        {
            throw new InvalidOperationException("Nama company sudah digunakan company lain.");
        }

        existing.CompanyName = company.CompanyName.Trim();
        await _adminRepository.UpdateCompanyAsync(existing, cancellationToken);
    }

    public async Task DeleteCompanyAsync(long companyId, CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            throw new InvalidOperationException("Company tidak valid.");
        }

        var existing = await _adminRepository.GetCompanyByIdAsync(companyId, cancellationToken);
        if (existing is null)
        {
            throw new InvalidOperationException("Company tidak ditemukan.");
        }

        var hasDependencies = await _adminRepository.CompanyHasDependenciesAsync(companyId, cancellationToken);
        if (hasDependencies)
        {
            throw new InvalidOperationException("Company tidak bisa dihapus karena masih dipakai oleh employee/vehicle/question.");
        }

        await _adminRepository.DeleteCompanyAsync(existing, cancellationToken);
    }

    public async Task<AuthUserDto?> ValidateUserAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _adminRepository.GetUserByUsernameAsync(request.Username, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return new AuthUserDto
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            CompanyId = user.CompanyId
        };
    }

    public Task<List<UserLogin>> GetUsersAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        return _adminRepository.GetUsersAsync(scope, cancellationToken);
    }

    public Task<UserLogin?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return _adminRepository.GetUserByIdAsync(userId, cancellationToken);
    }

    public async Task AddUserAsync(CreateUserDto user, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(user.Username))
        {
            throw new InvalidOperationException("Username wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(user.FullName))
        {
            throw new InvalidOperationException("Nama lengkap wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(user.Password) || user.Password.Length < 6)
        {
            throw new InvalidOperationException("Password minimal 6 karakter.");
        }

        ValidateRoleAndCompany(user.Role, user.CompanyId);

        var existing = await _adminRepository.GetUserByUsernameAsync(user.Username, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("Username sudah digunakan.");
        }

        var entity = new UserLogin
        {
            Username = user.Username.Trim().ToLowerInvariant(),
            FullName = user.FullName.Trim(),
            Role = user.Role,
            CompanyId = user.Role == SystemUserRole.Administrator ? null : user.CompanyId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        entity.PasswordHash = _passwordHasher.HashPassword(entity, user.Password);
        await _adminRepository.AddUserAsync(entity, cancellationToken);
    }

    public async Task UpdateUserAsync(UpdateUserDto user, CancellationToken cancellationToken = default)
    {
        if (user.UserId <= 0)
        {
            throw new InvalidOperationException("User tidak valid.");
        }

        if (string.IsNullOrWhiteSpace(user.FullName))
        {
            throw new InvalidOperationException("Nama lengkap wajib diisi.");
        }

        ValidateRoleAndCompany(user.Role, user.CompanyId);

        var existing = await _adminRepository.GetUserByIdAsync(user.UserId, cancellationToken);
        if (existing is null || !existing.IsActive)
        {
            throw new InvalidOperationException("User tidak ditemukan.");
        }

        existing.FullName = user.FullName.Trim();
        existing.Role = user.Role;
        existing.CompanyId = user.Role == SystemUserRole.Administrator ? null : user.CompanyId;
        await _adminRepository.UpdateUserAsync(existing, cancellationToken);
    }

    public async Task ResetUserPasswordAsync(ResetUserPasswordDto user, long? actorUserId = null, CancellationToken cancellationToken = default)
    {
        if (user.UserId <= 0)
        {
            throw new InvalidOperationException("User tidak valid.");
        }

        if (string.IsNullOrWhiteSpace(user.NewPassword) || user.NewPassword.Length < 6)
        {
            throw new InvalidOperationException("Password baru minimal 6 karakter.");
        }

        var existing = await _adminRepository.GetUserByIdAsync(user.UserId, cancellationToken);
        if (existing is null || !existing.IsActive)
        {
            throw new InvalidOperationException("User tidak ditemukan.");
        }

        existing.PasswordHash = _passwordHasher.HashPassword(existing, user.NewPassword);
        await _adminRepository.UpdateUserAsync(existing, cancellationToken);
    }

    public async Task DeleteUserAsync(long userId, long? actorUserId = null, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new InvalidOperationException("User tidak valid.");
        }

        if (actorUserId.HasValue && actorUserId.Value == userId)
        {
            throw new InvalidOperationException("Akun yang sedang login tidak bisa dihapus.");
        }

        var existing = await _adminRepository.GetUserByIdAsync(userId, cancellationToken);
        if (existing is null || !existing.IsActive)
        {
            throw new InvalidOperationException("User tidak ditemukan.");
        }

        if (existing.Role == SystemUserRole.Administrator)
        {
            var adminCount = await _adminRepository.CountActiveUsersByRoleAsync(SystemUserRole.Administrator, cancellationToken);
            if (adminCount <= 1)
            {
                throw new InvalidOperationException("Tidak bisa menghapus admin terakhir.");
            }
        }

        existing.IsActive = false;
        await _adminRepository.UpdateUserAsync(existing, cancellationToken);
    }

    public async Task ChangeOwnPasswordAsync(ProfilePasswordDto user, CancellationToken cancellationToken = default)
    {
        if (user.UserId <= 0)
        {
            throw new InvalidOperationException("User tidak valid.");
        }

        if (string.IsNullOrWhiteSpace(user.NewPassword) || user.NewPassword.Length < 6)
        {
            throw new InvalidOperationException("Password baru minimal 6 karakter.");
        }

        var existing = await _adminRepository.GetUserByIdAsync(user.UserId, cancellationToken);
        if (existing is null || !existing.IsActive)
        {
            throw new InvalidOperationException("User tidak ditemukan.");
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(existing, existing.PasswordHash, user.CurrentPassword);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            throw new InvalidOperationException("Password saat ini tidak sesuai.");
        }

        existing.PasswordHash = _passwordHasher.HashPassword(existing, user.NewPassword);
        await _adminRepository.UpdateUserAsync(existing, cancellationToken);
    }

    public Task<AnswerReviewDto?> GetAnswerReviewAsync(AccessScopeDto scope, long sessionId, CancellationToken cancellationToken = default)
    {
        return _adminRepository.GetAnswerReviewAsync(scope, sessionId, cancellationToken);
    }

    public async Task<ScheduleBoardDto> GetScheduleBoardAsync(AccessScopeDto scope, DateTime date, CancellationToken cancellationToken = default)
    {
        var selectedDate = date.Date;
        var employees = await _adminRepository.GetScheduleEmployeesAsync(scope, cancellationToken);
        var vehicles = await _adminRepository.GetScheduleVehiclesAsync(scope, cancellationToken);
        var items = await _adminRepository.GetScheduleItemsByDateAsync(scope, selectedDate, cancellationToken);

        return new ScheduleBoardDto
        {
            Date = selectedDate,
            IsAdministrator = scope.IsAdministrator,
            ScopedCompanyId = scope.CompanyId,
            MaxParticipantsPerSession = MaxParticipantsPerSession,
            Employees = employees,
            DisplayedEmployees = employees,
            TotalEmployeeCount = employees.Count,
            Vehicles = vehicles,
            Items = items
        };
    }

    public async Task<(bool Success, string Message, long? ScheduleId)> AssignScheduleAsync(AccessScopeDto scope, ScheduleAssignRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.EmployeeId <= 0 || request.VehicleId <= 0)
        {
            return (false, "Data penjadwalan tidak valid.", null);
        }

        var scheduleAt = request.ScheduledAt.Kind == DateTimeKind.Utc
            ? request.ScheduledAt
            : DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Local).ToUniversalTime();

        var employees = await _adminRepository.GetScheduleEmployeesAsync(scope, cancellationToken);
        if (!employees.Any(x => x.Id == request.EmployeeId))
        {
            return (false, "Peserta tidak ditemukan atau tidak memiliki akses.", null);
        }

        var vehicles = await _adminRepository.GetScheduleVehiclesAsync(scope, cancellationToken);
        var targetVehicle = vehicles.FirstOrDefault(x => x.Id == request.VehicleId);
        if (targetVehicle is null)
        {
            return (false, "Vehicle tidak ditemukan atau tidak memiliki akses.", null);
        }

        var targetEmployee = employees.First(x => x.Id == request.EmployeeId);
        if (scope.HasCompanyScope && targetEmployee.CompanyId != targetVehicle.CompanyId)
        {
            return (false, "Peserta dan vehicle harus berasal dari company yang sama.", null);
        }

        var sameAssignmentExists = await _adminRepository.ScheduleAssignmentExistsAsync(request.EmployeeId, request.VehicleId, scheduleAt, null, cancellationToken);
        if (sameAssignmentExists)
        {
            return (false, "Peserta ini sudah terjadwal pada sesi yang sama.", null);
        }

        var participantCount = await _adminRepository.GetScheduleParticipantCountAsync(request.VehicleId, scheduleAt, null, cancellationToken);
        if (participantCount >= MaxParticipantsPerSession)
        {
            return (false, $"Kapasitas sesi penuh. Maksimal {MaxParticipantsPerSession} peserta per sesi.", null);
        }

        var schedule = new ExamSchedule
        {
            EmployeeId = request.EmployeeId,
            VehicleId = request.VehicleId,
            ScheduledAt = scheduleAt,
            Status = "scheduled",
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        var scheduleId = await _adminRepository.AddScheduleAsync(schedule, cancellationToken);
        return (true, "Penjadwalan berhasil disimpan.", scheduleId);
    }

    public async Task<(bool Success, string Message)> MoveScheduleAsync(AccessScopeDto scope, ScheduleMoveRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.ScheduleId <= 0 || request.VehicleId <= 0)
        {
            return (false, "Data pemindahan jadwal tidak valid.");
        }

        var scheduleAt = request.ScheduledAt.Kind == DateTimeKind.Utc
            ? request.ScheduledAt
            : DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Local).ToUniversalTime();

        var vehicles = await _adminRepository.GetScheduleVehiclesAsync(scope, cancellationToken);
        var targetVehicle = vehicles.FirstOrDefault(x => x.Id == request.VehicleId);
        if (targetVehicle is null)
        {
            return (false, "Vehicle tujuan tidak ditemukan atau tidak memiliki akses.");
        }

        var existingSchedule = await _adminRepository.GetScheduleItemsByDateAsync(scope, request.ScheduledAt.Date, cancellationToken);
        var currentSchedule = existingSchedule.FirstOrDefault(x => x.Id == request.ScheduleId);
        if (currentSchedule is null)
        {
            return (false, "Jadwal tidak ditemukan atau tidak memiliki akses.");
        }

        var sameAssignmentExists = await _adminRepository.ScheduleAssignmentExistsAsync(currentSchedule.EmployeeId, request.VehicleId, scheduleAt, request.ScheduleId, cancellationToken);
        if (sameAssignmentExists)
        {
            return (false, "Peserta ini sudah ada pada sesi tujuan.");
        }

        var participantCount = await _adminRepository.GetScheduleParticipantCountAsync(request.VehicleId, scheduleAt, request.ScheduleId, cancellationToken);
        if (participantCount >= MaxParticipantsPerSession)
        {
            return (false, $"Sesi tujuan penuh. Maksimal {MaxParticipantsPerSession} peserta per sesi.");
        }

        var moved = await _adminRepository.MoveScheduleAsync(scope, request.ScheduleId, request.VehicleId, scheduleAt, cancellationToken);
        return moved
            ? (true, "Jadwal berhasil dipindahkan.")
            : (false, "Jadwal tidak ditemukan atau tidak memiliki akses.");
    }

    public Task<(bool Success, string Message)> RemoveScheduleAsync(AccessScopeDto scope, long scheduleId, CancellationToken cancellationToken = default)
    {
        return RemoveScheduleInternalAsync(scope, scheduleId, cancellationToken);
    }

    public async Task<(bool Success, string Message, int ScheduledCount)> AutoScheduleAsync(AccessScopeDto scope, AutoScheduleRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.PeoplePerDay <= 0)
        {
            return (false, "Setting orang per hari harus lebih dari 0.", 0);
        }

        var startDate = request.StartDate.Date;
        var employees = await _adminRepository.GetScheduleEmployeesAsync(scope, cancellationToken);
        var vehicles = await _adminRepository.GetScheduleVehiclesAsync(scope, cancellationToken);
        if (employees.Count == 0 || vehicles.Count == 0)
        {
            return (false, "Data employee atau vehicle belum tersedia.", 0);
        }

        var scheduledEmployeeIds = await _adminRepository.GetEmployeeIdsWithAnyScheduleAsync(scope, cancellationToken);
        var unscheduled = employees.Where(x => !scheduledEmployeeIds.Contains(x.Id)).OrderBy(x => x.Nrp).ToList();
        if (unscheduled.Count == 0)
        {
            return (true, "Semua NIK sudah memiliki jadwal.", 0);
        }

        var slotHours = Enumerable.Range(8, 10).ToList();
        var maxPerDay = Math.Min(request.PeoplePerDay, slotHours.Count * vehicles.Count);
        var pointer = 0;
        var dayOffset = 0;
        var totalAssigned = 0;

        while (pointer < unscheduled.Count)
        {
            var dayDate = startDate.AddDays(dayOffset);
            var assignedToday = 0;

            foreach (var hour in slotHours)
            {
                foreach (var vehicle in vehicles)
                {
                    if (assignedToday >= maxPerDay || pointer >= unscheduled.Count)
                    {
                        break;
                    }

                    var localSlot = dayDate.AddHours(hour);
                    var utcSlot = DateTime.SpecifyKind(localSlot, DateTimeKind.Local).ToUniversalTime();
                    var participantCount = await _adminRepository.GetScheduleParticipantCountAsync(vehicle.Id, utcSlot, null, cancellationToken);
                    if (participantCount >= MaxParticipantsPerSession)
                    {
                        continue;
                    }

                    var employee = unscheduled[pointer];
                    await _adminRepository.AddScheduleAsync(new ExamSchedule
                    {
                        EmployeeId = employee.Id,
                        VehicleId = vehicle.Id,
                        ScheduledAt = utcSlot,
                        Status = "scheduled",
                        CreatedByUserId = null,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);

                    pointer++;
                    assignedToday++;
                    totalAssigned++;
                }

                if (assignedToday >= maxPerDay || pointer >= unscheduled.Count)
                {
                    break;
                }
            }

            dayOffset++;
        }

        return (true, $"Auto penjadwalan berhasil. Total terjadwal: {totalAssigned} peserta.", totalAssigned);
    }

    public Task<List<UserLogin>> GetInstructorsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        return _adminRepository.GetInstructorsAsync(scope, cancellationToken);
    }

    public async Task<List<PracticalTemplateSummaryDto>> GetPracticalTemplatesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        var templates = await _adminRepository.GetPracticalTemplatesAsync(scope, cancellationToken);
        return templates.Select(MapPracticalTemplateSummary).ToList();
    }

    public async Task CreatePracticalTemplateAsync(AccessScopeDto scope, PracticalTemplateUpsertDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAndNormalizePracticalTemplateRequestAsync(scope, request, cancellationToken);

        var existingTemplates = await _adminRepository.GetPracticalTemplatesAsync(scope, cancellationToken);
        var activeMatches = existingTemplates
            .Where(x => x.VehicleId == request.VehicleId && x.CompanyId == request.CompanyId && x.IsActive)
            .ToList();

        foreach (var activeTemplate in activeMatches)
        {
            activeTemplate.IsActive = false;
        }

        var entity = new PracticalAssessmentTemplate
        {
            CompanyId = request.CompanyId,
            VehicleId = request.VehicleId,
            TemplateName = request.TemplateName.Trim(),
            ScoringMode = request.ScoringMode,
            PassingScore = request.PassingScore,
            PassingGrade = request.PassingGrade,
            GradeOptions = request.GradeOptions,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var templateId = await _adminRepository.AddPracticalTemplateAsync(entity, cancellationToken);
        var items = request.Items
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemLabel))
            .Select((x, index) => new PracticalAssessmentTemplateItem
            {
                TemplateId = templateId,
                SectionName = string.IsNullOrWhiteSpace(x.SectionName) ? "General" : x.SectionName.Trim(),
                ItemLabel = x.ItemLabel.Trim(),
                Weight = x.Weight <= 0 ? 1 : x.Weight,
                DisplayOrder = index + 1
            })
            .ToList();

        await _adminRepository.AddPracticalTemplateItemsAsync(items, cancellationToken);
    }

    public async Task UpdatePracticalTemplateAsync(AccessScopeDto scope, long templateId, PracticalTemplateUpsertDto request, CancellationToken cancellationToken = default)
    {
        var template = await _adminRepository.GetPracticalTemplateByIdAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Template praktek tidak ditemukan.");

        EnsurePracticalTemplateInScope(scope, template);
        if (template.Sessions.Any())
        {
            throw new InvalidOperationException("Template yang sudah dipakai penilaian tidak bisa diedit. Buat template baru sebagai versi berikutnya.");
        }

        await ValidateAndNormalizePracticalTemplateRequestAsync(scope, request, cancellationToken);

        template.CompanyId = request.CompanyId;
        template.VehicleId = request.VehicleId;
        template.TemplateName = request.TemplateName.Trim();
        template.ScoringMode = request.ScoringMode;
        template.PassingScore = request.PassingScore;
        template.PassingGrade = request.PassingGrade;
        template.GradeOptions = request.GradeOptions;

        if (template.IsActive)
        {
            var existingTemplates = await _adminRepository.GetPracticalTemplatesAsync(scope, cancellationToken);
            var activeMatches = existingTemplates
                .Where(x => x.Id != template.Id && x.VehicleId == request.VehicleId && x.CompanyId == request.CompanyId && x.IsActive)
                .ToList();

            foreach (var activeTemplate in activeMatches)
            {
                activeTemplate.IsActive = false;
            }
        }

        await _adminRepository.UpdatePracticalTemplateAsync(template, cancellationToken);

        var items = request.Items
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemLabel))
            .Select((x, index) => new PracticalAssessmentTemplateItem
            {
                TemplateId = template.Id,
                SectionName = string.IsNullOrWhiteSpace(x.SectionName) ? "General" : x.SectionName.Trim(),
                ItemLabel = x.ItemLabel.Trim(),
                Weight = x.Weight <= 0 ? 1 : x.Weight,
                DisplayOrder = index + 1
            })
            .ToList();

        await _adminRepository.ReplacePracticalTemplateItemsAsync(template.Id, items, cancellationToken);
    }

    public async Task SetPracticalTemplateStatusAsync(AccessScopeDto scope, long templateId, bool isActive, CancellationToken cancellationToken = default)
    {
        var template = await _adminRepository.GetPracticalTemplateByIdAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Template praktek tidak ditemukan.");

        EnsurePracticalTemplateInScope(scope, template);

        if (isActive)
        {
            var templates = await _adminRepository.GetPracticalTemplatesAsync(scope, cancellationToken);
            var activeMatches = templates
                .Where(x => x.Id != template.Id && x.CompanyId == template.CompanyId && x.VehicleId == template.VehicleId && x.IsActive)
                .ToList();

            foreach (var activeTemplate in activeMatches)
            {
                activeTemplate.IsActive = false;
            }
        }

        template.IsActive = isActive;
        await _adminRepository.UpdatePracticalTemplateAsync(template, cancellationToken);
    }

    public async Task DeletePracticalTemplateAsync(AccessScopeDto scope, long templateId, CancellationToken cancellationToken = default)
    {
        var template = await _adminRepository.GetPracticalTemplateByIdAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Template praktek tidak ditemukan.");

        EnsurePracticalTemplateInScope(scope, template);
        if (template.Sessions.Any())
        {
            throw new InvalidOperationException("Template yang sudah dipakai penilaian tidak bisa dihapus.");
        }

        await _adminRepository.DeletePracticalTemplateAsync(template, cancellationToken);
    }

    public async Task<List<PracticalSessionSummaryDto>> GetPracticalSessionsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        var sessions = await _adminRepository.GetPracticalSessionsAsync(scope, cancellationToken);
        var summaries = sessions.Select(MapPracticalSessionSummary).ToList();
        await EnrichPracticalSummariesAsync(summaries, cancellationToken);
        return summaries;
    }

    public async Task CreatePracticalSessionAsync(AccessScopeDto scope, PracticalSessionCreateDto request, CancellationToken cancellationToken = default)
    {
        if (request.EmployeeId <= 0 || request.VehicleId <= 0 || request.TemplateId <= 0 || request.InstructorUserId <= 0)
        {
            throw new InvalidOperationException("Semua field penjadwalan praktek wajib diisi.");
        }

        var employees = await _adminRepository.GetEmployeesAsync(scope, cancellationToken);
        var employee = employees.FirstOrDefault(x => x.Id == request.EmployeeId)
            ?? throw new InvalidOperationException("Peserta praktek tidak ditemukan pada scope login.");

        var vehicles = await _adminRepository.GetVehiclesAsync(scope, cancellationToken);
        var vehicle = vehicles.FirstOrDefault(x => x.Id == request.VehicleId)
            ?? throw new InvalidOperationException("Unit praktek tidak ditemukan pada scope login.");

        if (employee.CompanyId != vehicle.CompanyId)
        {
            throw new InvalidOperationException("Peserta dan unit praktek harus dari company yang sama.");
        }

        var instructors = await _adminRepository.GetInstructorsAsync(scope, cancellationToken);
        var instructor = instructors.FirstOrDefault(x => x.Id == request.InstructorUserId)
            ?? throw new InvalidOperationException("Instruktur tidak ditemukan pada scope login.");

        var template = await _adminRepository.GetPracticalTemplateByIdAsync(request.TemplateId, cancellationToken)
            ?? throw new InvalidOperationException("Template praktek tidak ditemukan.");

        if (!template.IsActive)
        {
            throw new InvalidOperationException("Template praktek yang dipilih sudah tidak aktif.");
        }

        if (template.VehicleId != request.VehicleId || template.CompanyId != employee.CompanyId)
        {
            throw new InvalidOperationException("Template praktek tidak cocok dengan peserta atau unit yang dipilih.");
        }

        var scheduleAt = request.ScheduledAt.Kind == DateTimeKind.Utc
            ? request.ScheduledAt
            : DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Local).ToUniversalTime();

        var entity = new PracticalAssessmentSession
        {
            EmployeeId = request.EmployeeId,
            VehicleId = request.VehicleId,
            TemplateId = request.TemplateId,
            InstructorUserId = request.InstructorUserId,
            ScheduledAt = scheduleAt,
            Status = PracticalAssessmentStatus.Scheduled,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _adminRepository.AddPracticalSessionAsync(entity, cancellationToken);
    }

    public async Task<List<PracticalSessionSummaryDto>> GetInstructorPracticalSessionsAsync(long instructorUserId, CancellationToken cancellationToken = default)
    {
        var sessions = await _adminRepository.GetInstructorPracticalSessionsAsync(instructorUserId, cancellationToken);
        var summaries = sessions.Select(MapPracticalSessionSummary).ToList();
        await EnrichPracticalSummariesAsync(summaries, cancellationToken);
        return summaries;
    }

    public async Task<PracticalEvaluationDto?> GetPracticalEvaluationAsync(AccessScopeDto scope, long sessionId, long? currentUserId, bool isInstructor, CancellationToken cancellationToken = default)
    {
        var session = await _adminRepository.GetPracticalSessionByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (!CanAccessPracticalSession(scope, session, currentUserId, isInstructor))
        {
            return null;
        }

        var gradeOptions = NormalizeGradeOptions(session.Template?.GradeOptions);
        var evaluation = new PracticalEvaluationDto
        {
            SessionId = session.Id,
            Status = session.Status,
            EmployeeName = session.Employee?.EmployeeName ?? string.Empty,
            Nrp = session.Employee?.Nrp ?? string.Empty,
            VehicleName = session.Vehicle?.VehicleName ?? string.Empty,
            CompanyName = session.Employee?.Company?.CompanyName ?? string.Empty,
            TemplateName = session.Template?.TemplateName ?? string.Empty,
            ScoringMode = session.Template?.ScoringMode ?? string.Empty,
            PassingScore = session.Template?.PassingScore,
            PassingGrade = session.Template?.PassingGrade,
            GradeOptions = string.Join(",", gradeOptions),
            InstructorName = session.InstructorUser?.FullName ?? string.Empty,
            ScheduledAt = session.ScheduledAt,
            FinalNumericScore = session.FinalNumericScore,
            FinalGrade = session.FinalGrade,
            PassStatus = session.PassStatus,
            InstructorNote = session.InstructorNote,
            Items = session.Template?.Items
                .OrderBy(x => x.DisplayOrder)
                .Select(item =>
                {
                    var score = session.Scores.FirstOrDefault(x => x.TemplateItemId == item.Id);
                    return new PracticalEvaluationItemDto
                    {
                        TemplateItemId = item.Id,
                        SectionName = item.SectionName ?? "General",
                        ItemLabel = item.ItemLabel,
                        Weight = item.Weight,
                        NumericValue = score?.NumericValue,
                        GradeValue = score?.GradeValue,
                        Note = score?.Note
                    };
                })
                .ToList() ?? new List<PracticalEvaluationItemDto>()
        };

        await EnrichPracticalEvaluationAsync(evaluation, cancellationToken);
        return evaluation;
    }

    public async Task<SummaryScoreDto?> GetSummaryScoreAsync(AccessScopeDto scope, long theorySessionId, long practicalSessionId, long? currentUserId, bool isInstructor, CancellationToken cancellationToken = default)
    {
        var theorySession = await _adminRepository.GetExamSessionByIdAsync(theorySessionId, cancellationToken);
        var practicalSession = await _adminRepository.GetPracticalSessionByIdAsync(practicalSessionId, cancellationToken);

        if (theorySession is null || practicalSession is null)
        {
            return null;
        }

        if (!CanAccessTheorySession(scope, theorySession) || !CanAccessPracticalSession(scope, practicalSession, currentUserId, isInstructor))
        {
            return null;
        }

        if (theorySession.EmployeeId != practicalSession.EmployeeId || theorySession.VehicleId != practicalSession.VehicleId)
        {
            return null;
        }

        if (theorySession.ExamResult is null || !practicalSession.PassStatus.HasValue)
        {
            return null;
        }

        var applicant = await ResolveApplicantByNikAsync(theorySession.Employee?.Nrp, cancellationToken);
        var scoringMode = practicalSession.Template?.ScoringMode ?? string.Empty;

        return new SummaryScoreDto
        {
            IsComplete = true,
            SummaryId = $"TH-{theorySession.Id}-PR-{practicalSession.Id}",
            CompanyName = theorySession.Employee?.Company?.CompanyName ?? applicant?.Perusahaan ?? "PT INDEXIM COALINDO",
            EmployeeName = theorySession.Employee?.EmployeeName ?? string.Empty,
            Nrp = theorySession.Employee?.Nrp ?? string.Empty,
            Ktp = applicant?.Ktp?.Trim() ?? string.Empty,
            SubmissionNumber = applicant?.Nomor?.Trim() ?? string.Empty,
            SubmissionType = applicant?.Pengajuan?.Trim() ?? string.Empty,
            DepartmentName = applicant?.Departemen?.Trim() ?? string.Empty,
            VehicleName = theorySession.Vehicle?.VehicleName ?? string.Empty,
            InstructorName = practicalSession.InstructorUser?.FullName ?? string.Empty,
            TheoryScheduledAt = theorySession.StartTime ?? theorySession.CreatedAt,
            TheoryFinishedAt = theorySession.ExamResult.FinishedAt,
            PracticalScheduledAt = practicalSession.ScheduledAt,
            PracticalFinishedAt = practicalSession.SubmittedAt,
            TheoryScore = theorySession.ExamResult.Score,
            TheoryPassed = theorySession.ExamResult.PassStatus,
            TheoryTotalQuestions = theorySession.ExamResult.TotalQuestions,
            TheoryCorrectAnswers = theorySession.ExamResult.CorrectAnswers,
            PracticalNumericScore = practicalSession.FinalNumericScore,
            PracticalGrade = practicalSession.FinalGrade,
            PracticalPassed = practicalSession.PassStatus ?? false,
            PracticalTemplateName = practicalSession.Template?.TemplateName ?? string.Empty,
            PracticalScoringMode = scoringMode,
            InstructorNote = practicalSession.InstructorNote,
            TheorySessionId = theorySession.Id,
            PracticalSessionId = practicalSession.Id,
            PracticalItems = practicalSession.Template?.Items
                .OrderBy(x => x.DisplayOrder)
                .Select(item =>
                {
                    var score = practicalSession.Scores.FirstOrDefault(x => x.TemplateItemId == item.Id);
                    return new SummaryPracticalItemDto
                    {
                        SectionName = item.SectionName ?? "General",
                        ItemLabel = item.ItemLabel,
                        Weight = item.Weight,
                        NumericValue = score?.NumericValue,
                        GradeValue = score?.GradeValue,
                        Note = score?.Note
                    };
                })
                .ToList() ?? new List<SummaryPracticalItemDto>()
        };
    }

    public async Task SubmitPracticalEvaluationAsync(AccessScopeDto scope, PracticalEvaluationSubmitDto request, long? currentUserId, bool isInstructor, CancellationToken cancellationToken = default)
    {
        var session = await _adminRepository.GetPracticalSessionByIdAsync(request.SessionId, cancellationToken)
            ?? throw new InvalidOperationException("Sesi praktek tidak ditemukan.");

        if (!CanAccessPracticalSession(scope, session, currentUserId, isInstructor))
        {
            throw new InvalidOperationException("Anda tidak memiliki akses ke form penilaian ini.");
        }

        var template = session.Template ?? throw new InvalidOperationException("Template praktek tidak ditemukan.");
        var items = template.Items.OrderBy(x => x.DisplayOrder).ToList();
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Template praktek belum memiliki item penilaian.");
        }

        var gradeOptions = NormalizeGradeOptions(template.GradeOptions);
        var submittedScores = new List<PracticalAssessmentScore>();

        foreach (var item in items)
        {
            var submitted = request.Items.FirstOrDefault(x => x.TemplateItemId == item.Id)
                ?? throw new InvalidOperationException($"Nilai untuk item {item.ItemLabel} belum diisi.");

            decimal? numericValue = null;
            string? gradeValue = null;

            if (template.ScoringMode == PracticalScoringMode.Numeric)
            {
                if (!submitted.NumericValue.HasValue)
                {
                    throw new InvalidOperationException($"Nilai angka untuk item {item.ItemLabel} wajib diisi.");
                }

                if (submitted.NumericValue < 0 || submitted.NumericValue > 100)
                {
                    throw new InvalidOperationException($"Nilai angka untuk item {item.ItemLabel} harus di antara 0 sampai 100.");
                }

                numericValue = submitted.NumericValue.Value;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(submitted.GradeValue))
                {
                    throw new InvalidOperationException($"Nilai huruf untuk item {item.ItemLabel} wajib diisi.");
                }

                gradeValue = submitted.GradeValue.Trim().ToUpperInvariant();
                if (!gradeOptions.Contains(gradeValue))
                {
                    throw new InvalidOperationException($"Nilai huruf {gradeValue} tidak valid untuk item {item.ItemLabel}.");
                }
            }

            submittedScores.Add(new PracticalAssessmentScore
            {
                SessionId = session.Id,
                TemplateItemId = item.Id,
                NumericValue = numericValue,
                GradeValue = gradeValue,
                Note = submitted.Note?.Trim()
            });
        }

        await _adminRepository.UpsertPracticalScoresAsync(session.Id, submittedScores, cancellationToken);

        if (template.ScoringMode == PracticalScoringMode.Numeric)
        {
            var finalNumericScore = CalculateWeightedNumericScore(items, submittedScores);
            session.FinalNumericScore = finalNumericScore;
            session.FinalGrade = null;
            session.PassStatus = finalNumericScore >= (template.PassingScore ?? 0);
        }
        else
        {
            var finalGrade = CalculateWeightedGrade(items, submittedScores, gradeOptions);
            session.FinalGrade = finalGrade;
            session.FinalNumericScore = null;
            session.PassStatus = CompareGrade(finalGrade, template.PassingGrade ?? string.Empty, gradeOptions) >= 0;
        }

        session.InstructorNote = request.InstructorNote?.Trim();
        session.SubmittedAt = DateTime.UtcNow;
        session.Status = PracticalAssessmentStatus.Submitted;

        await _adminRepository.UpdatePracticalSessionAsync(session, cancellationToken);
    }

    public async Task<(bool Success, string Message)> MovePracticalSessionAsync(AccessScopeDto scope, PracticalSessionMoveDto request, CancellationToken cancellationToken = default)
    {
        if (request.SessionId <= 0 || request.VehicleId <= 0)
        {
            return (false, "Data pemindahan jadwal praktek tidak valid.");
        }

        var existingSession = await _adminRepository.GetPracticalSessionByIdAsync(request.SessionId, cancellationToken);
        if (existingSession is null)
        {
            return (false, "Jadwal praktek tidak ditemukan.");
        }

        if (!scope.IsAdministrator && scope.HasCompanyScope && existingSession.Employee?.CompanyId != scope.CompanyId)
        {
            return (false, "Jadwal praktek tidak ditemukan pada scope login.");
        }

        if (existingSession.VehicleId != request.VehicleId)
        {
            return (false, "Perpindahan unit praktek tidak diizinkan dari board. Gunakan form baru jika unit berubah karena template penilaian berbeda.");
        }

        var scheduleAt = request.ScheduledAt.Kind == DateTimeKind.Utc
            ? request.ScheduledAt
            : DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Local).ToUniversalTime();

        var vehicles = await _adminRepository.GetVehiclesAsync(scope, cancellationToken);
        var vehicle = vehicles.FirstOrDefault(x => x.Id == request.VehicleId);
        if (vehicle is null)
        {
            return (false, "Unit praktek tujuan tidak ditemukan pada scope login.");
        }

        var moved = await _adminRepository.MovePracticalSessionAsync(scope, request.SessionId, request.VehicleId, scheduleAt, cancellationToken);
        return moved
            ? (true, "Jadwal praktek berhasil dipindahkan.")
            : (false, "Jadwal praktek tidak ditemukan atau tidak memiliki akses.");
    }

    public Task<(bool Success, string Message)> RemovePracticalSessionAsync(AccessScopeDto scope, long sessionId, CancellationToken cancellationToken = default)
    {
        return RemovePracticalSessionInternalAsync(scope, sessionId, cancellationToken);
    }

    private async Task<(bool Success, string Message)> RemoveScheduleInternalAsync(AccessScopeDto scope, long scheduleId, CancellationToken cancellationToken)
    {
        var removed = await _adminRepository.RemoveScheduleAsync(scope, scheduleId, cancellationToken);
        return removed
            ? (true, "Jadwal berhasil dihapus.")
            : (false, "Jadwal tidak ditemukan atau tidak memiliki akses.");
    }

    private async Task<(bool Success, string Message)> RemovePracticalSessionInternalAsync(AccessScopeDto scope, long sessionId, CancellationToken cancellationToken)
    {
        var removed = await _adminRepository.RemovePracticalSessionAsync(scope, sessionId, cancellationToken);
        return removed
            ? (true, "Jadwal praktek berhasil dihapus.")
            : (false, "Jadwal praktek tidak ditemukan atau tidak memiliki akses.");
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static PracticalTemplateSummaryDto MapPracticalTemplateSummary(PracticalAssessmentTemplate template)
    {
        return new PracticalTemplateSummaryDto
        {
            Id = template.Id,
            CompanyId = template.CompanyId,
            CompanyName = template.Company?.CompanyName ?? string.Empty,
            VehicleId = template.VehicleId,
            VehicleName = template.Vehicle?.VehicleName ?? string.Empty,
            TemplateName = template.TemplateName,
            ScoringMode = template.ScoringMode,
            PassingScore = template.PassingScore,
            PassingGrade = template.PassingGrade,
            GradeOptions = template.GradeOptions,
            IsActive = template.IsActive,
            SessionCount = template.Sessions.Count,
            CreatedAt = template.CreatedAt,
            Items = template.Items
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new PracticalTemplateItemDto
                {
                    Id = x.Id,
                    SectionName = x.SectionName ?? "General",
                    ItemLabel = x.ItemLabel,
                    Weight = x.Weight,
                    DisplayOrder = x.DisplayOrder
                })
                .ToList()
        };
    }

    private static PracticalSessionSummaryDto MapPracticalSessionSummary(PracticalAssessmentSession session)
    {
        return new PracticalSessionSummaryDto
        {
            Id = session.Id,
            EmployeeId = session.EmployeeId,
            EmployeeName = session.Employee?.EmployeeName ?? string.Empty,
            Nrp = session.Employee?.Nrp ?? string.Empty,
            CompanyId = session.Employee?.CompanyId ?? 0,
            CompanyName = session.Employee?.Company?.CompanyName ?? string.Empty,
            VehicleId = session.VehicleId,
            VehicleName = session.Vehicle?.VehicleName ?? string.Empty,
            TemplateId = session.TemplateId,
            TemplateName = session.Template?.TemplateName ?? string.Empty,
            InstructorUserId = session.InstructorUserId,
            InstructorName = session.InstructorUser?.FullName ?? string.Empty,
            ScheduledAt = session.ScheduledAt,
            Status = session.Status,
            FinalNumericScore = session.FinalNumericScore,
            FinalGrade = session.FinalGrade,
            PassStatus = session.PassStatus,
            InstructorNote = session.InstructorNote,
            SubmittedAt = session.SubmittedAt
        };
    }

    private static List<string> NormalizeGradeOptions(string? rawOptions)
    {
        return (rawOptions ?? "A,B,C,D")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task ValidateAndNormalizePracticalTemplateRequestAsync(AccessScopeDto scope, PracticalTemplateUpsertDto request, CancellationToken cancellationToken)
    {
        if (request.VehicleId <= 0 || request.CompanyId <= 0)
        {
            throw new InvalidOperationException("Company dan unit wajib dipilih.");
        }

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            request.CompanyId = scope.CompanyId!.Value;
        }

        if (string.IsNullOrWhiteSpace(request.TemplateName))
        {
            throw new InvalidOperationException("Nama template wajib diisi.");
        }

        if (request.ScoringMode is not (PracticalScoringMode.Numeric or PracticalScoringMode.Grade))
        {
            throw new InvalidOperationException("Mode penilaian praktek tidak valid.");
        }

        request.Items = request.Items
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemLabel))
            .ToList();

        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("Minimal satu item penilaian harus dibuat.");
        }

        if (request.ScoringMode == PracticalScoringMode.Numeric)
        {
            if (!request.PassingScore.HasValue)
            {
                throw new InvalidOperationException("Nilai lulus angka wajib diisi.");
            }

            request.PassingGrade = null;
            request.GradeOptions = null;
        }
        else
        {
            var gradeOptions = NormalizeGradeOptions(request.GradeOptions);
            if (gradeOptions.Count < 2)
            {
                throw new InvalidOperationException("Mode huruf memerlukan minimal dua grade option.");
            }

            if (string.IsNullOrWhiteSpace(request.PassingGrade) || !gradeOptions.Contains(request.PassingGrade.Trim().ToUpperInvariant()))
            {
                throw new InvalidOperationException("Passing grade harus ada di daftar grade option.");
            }

            request.PassingScore = null;
            request.PassingGrade = request.PassingGrade.Trim().ToUpperInvariant();
            request.GradeOptions = string.Join(",", gradeOptions);
        }

        var vehicles = await _adminRepository.GetVehiclesAsync(scope, cancellationToken);
        var vehicle = vehicles.FirstOrDefault(x => x.Id == request.VehicleId);
        if (vehicle is null)
        {
            throw new InvalidOperationException("Unit tidak ditemukan pada scope login.");
        }

        if (vehicle.CompanyId != request.CompanyId)
        {
            throw new InvalidOperationException("Unit tidak sesuai dengan company yang dipilih.");
        }
    }

    private static void EnsurePracticalTemplateInScope(AccessScopeDto scope, PracticalAssessmentTemplate template)
    {
        if (!scope.IsAdministrator && scope.HasCompanyScope && template.CompanyId != scope.CompanyId)
        {
            throw new InvalidOperationException("Template praktek tidak ditemukan pada scope login.");
        }
    }

    private static decimal CalculateWeightedNumericScore(List<PracticalAssessmentTemplateItem> items, List<PracticalAssessmentScore> scores)
    {
        var totalWeight = items.Sum(x => x.Weight <= 0 ? 1 : x.Weight);
        if (totalWeight <= 0)
        {
            return 0;
        }

        decimal weightedTotal = 0;
        foreach (var item in items)
        {
            var score = scores.First(x => x.TemplateItemId == item.Id);
            weightedTotal += (score.NumericValue ?? 0) * (item.Weight <= 0 ? 1 : item.Weight);
        }

        return Math.Round(weightedTotal / totalWeight, 2);
    }

    private static string CalculateWeightedGrade(List<PracticalAssessmentTemplateItem> items, List<PracticalAssessmentScore> scores, List<string> gradeOptions)
    {
        var totalWeight = items.Sum(x => x.Weight <= 0 ? 1 : x.Weight);
        if (totalWeight <= 0)
        {
            return gradeOptions.LastOrDefault() ?? "D";
        }

        decimal weightedPoints = 0;
        foreach (var item in items)
        {
            var score = scores.First(x => x.TemplateItemId == item.Id);
            weightedPoints += GetGradePoint(score.GradeValue, gradeOptions) * (item.Weight <= 0 ? 1 : item.Weight);
        }

        var averagePoint = weightedPoints / totalWeight;
        return MapAveragePointToGrade(averagePoint, gradeOptions);
    }

    private static int CompareGrade(string actual, string required, List<string> gradeOptions)
    {
        return GetGradePoint(actual, gradeOptions).CompareTo(GetGradePoint(required, gradeOptions));
    }

    private static decimal GetGradePoint(string? grade, List<string> gradeOptions)
    {
        if (string.IsNullOrWhiteSpace(grade))
        {
            return 0;
        }

        var index = gradeOptions.FindIndex(x => x.Equals(grade.Trim(), StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return 0;
        }

        return gradeOptions.Count - index;
    }

    private static string MapAveragePointToGrade(decimal averagePoint, List<string> gradeOptions)
    {
        if (gradeOptions.Count == 0)
        {
            return string.Empty;
        }

        var rounded = (int)Math.Round(averagePoint, MidpointRounding.AwayFromZero);
        var normalizedIndex = Math.Clamp(gradeOptions.Count - rounded, 0, gradeOptions.Count - 1);
        return gradeOptions[normalizedIndex];
    }

    private static bool CanAccessPracticalSession(AccessScopeDto scope, PracticalAssessmentSession session, long? currentUserId, bool isInstructor)
    {
        if (isInstructor)
        {
            return currentUserId.HasValue && session.InstructorUserId == currentUserId.Value;
        }

        if (scope.IsAdministrator)
        {
            return true;
        }

        return scope.HasCompanyScope && session.Employee?.CompanyId == scope.CompanyId;
    }

    private static bool CanAccessTheorySession(AccessScopeDto scope, ExamSession session)
    {
        if (scope.IsAdministrator)
        {
            return true;
        }

        return scope.HasCompanyScope && session.Employee?.CompanyId == scope.CompanyId;
    }

    private async Task EnrichPracticalSummariesAsync(List<PracticalSessionSummaryDto> sessions, CancellationToken cancellationToken)
    {
        foreach (var session in sessions)
        {
            var applicant = await ResolveApplicantByNikAsync(session.Nrp, cancellationToken);
            if (applicant is null)
            {
                continue;
            }

            session.Ktp = applicant.Ktp?.Trim();
            session.SubmissionNumber = applicant.Nomor?.Trim();
            session.SubmissionType = applicant.Pengajuan?.Trim();
            session.DepartmentName = applicant.Departemen?.Trim();
            if (string.IsNullOrWhiteSpace(session.CompanyName))
            {
                session.CompanyName = applicant.Perusahaan?.Trim() ?? string.Empty;
            }
        }
    }

    private async Task EnrichPracticalEvaluationAsync(PracticalEvaluationDto evaluation, CancellationToken cancellationToken)
    {
        var applicant = await ResolveApplicantByNikAsync(evaluation.Nrp, cancellationToken);
        if (applicant is null)
        {
            return;
        }

        evaluation.Ktp = applicant.Ktp?.Trim();
        evaluation.SubmissionNumber = applicant.Nomor?.Trim();
        evaluation.SubmissionType = applicant.Pengajuan?.Trim();
        evaluation.DepartmentName = applicant.Departemen?.Trim();
        if (string.IsNullOrWhiteSpace(evaluation.CompanyName))
        {
            evaluation.CompanyName = applicant.Perusahaan?.Trim() ?? string.Empty;
        }
    }

    private async Task<SimperApplicantView?> ResolveApplicantByNikAsync(string? nrp, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nrp))
        {
            return null;
        }

        return await _adminRepository.GetLatestApplicantByNikAsync(nrp.Trim(), cancellationToken);
    }

    private static void ValidateRoleAndCompany(string role, long? companyId)
    {
        if (role != SystemUserRole.Administrator &&
            role != SystemUserRole.CompanyAdmin &&
            role != SystemUserRole.Instructor)
        {
            throw new InvalidOperationException("Role user tidak valid.");
        }

        if ((role == SystemUserRole.CompanyAdmin || role == SystemUserRole.Instructor) && !companyId.HasValue)
        {
            throw new InvalidOperationException("Company wajib diisi untuk role CompanyAdmin atau Instructor.");
        }
    }
}
