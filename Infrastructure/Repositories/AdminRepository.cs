using Microsoft.EntityFrameworkCore;
using Npgsql;
using SimperSecureOnlineTestSystem.Application.DTOs;
using SimperSecureOnlineTestSystem.Domain.Entities;
using SimperSecureOnlineTestSystem.Domain.Enums;
using SimperSecureOnlineTestSystem.Infrastructure.Data;

namespace SimperSecureOnlineTestSystem.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly ApplicationDbContext _db;

    public AdminRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<List<AdminExamMonitorDto>> GetActiveExamMonitorsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(new List<AdminExamMonitorDto>());
        }

        var query = _db.ExamSessions
            .Where(x => x.Status == ExamSessionStatus.Pending || x.Status == ExamSessionStatus.Active)
            .AsQueryable();

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.Employee!.CompanyId == scope.CompanyId!.Value);
        }

        return query.OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminExamMonitorDto
            {
                SessionId = x.Id,
                Token = x.Token,
                RefId = x.RefId,
                CompanyId = x.Employee!.CompanyId,
                CompanyName = x.Employee.Company!.CompanyName,
                VehicleId = x.VehicleId,
                Nrp = x.Employee!.Nrp,
                EmployeeName = x.Employee!.EmployeeName,
                VehicleName = x.Vehicle!.VehicleName,
                Status = x.Status.ToString(),
                CameraActive = x.CameraActive,
                TabSwitchCount = x.TabSwitchCount,
                AnsweredCount = x.ExamAnswers.Count(a => a.SelectedAnswer != null),
                TotalQuestions = x.ExamQuestions.Count,
                Score = x.ExamResult != null ? x.ExamResult.Score : 0
            })
            .ToListAsync(cancellationToken);
    }

    public Task<List<Company>> GetCompaniesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(new List<Company>());
        }

        var query = _db.CompanyDirectories.AsNoTracking().AsQueryable();
        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.Id == scope.CompanyId!.Value);
        }

        return query
            .Where(x => x.DeletedAt == null)
            .OrderBy(x => x.CompanyName)
            .Select(x => new Company
            {
                Id = x.Id,
                CompanyName = x.CompanyName ?? string.Empty
            })
            .ToListAsync(cancellationToken);
    }

    public Task<Company?> GetCompanyByIdAsync(long companyId, CancellationToken cancellationToken = default)
    {
        return _db.Companies.FirstOrDefaultAsync(x => x.Id == companyId, cancellationToken);
    }

    public Task<Company?> GetCompanyByNameAsync(string companyName, CancellationToken cancellationToken = default)
    {
        var normalized = companyName.Trim().ToLowerInvariant();
        return _db.Companies.FirstOrDefaultAsync(x => x.CompanyName.ToLower() == normalized, cancellationToken);
    }

    public async Task AddCompanyAsync(Company company, CancellationToken cancellationToken = default)
    {
        await _db.Companies.AddAsync(company, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCompanyAsync(Company company, CancellationToken cancellationToken = default)
    {
        _db.Companies.Update(company);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCompanyAsync(Company company, CancellationToken cancellationToken = default)
    {
        _db.Companies.Remove(company);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CompanyHasDependenciesAsync(long companyId, CancellationToken cancellationToken = default)
    {
        var hasEmployees = await _db.Employees.AnyAsync(x => x.CompanyId == companyId, cancellationToken);
        if (hasEmployees)
        {
            return true;
        }

        var hasVehicles = await _db.Vehicles.AnyAsync(x => x.CompanyId == companyId, cancellationToken);
        if (hasVehicles)
        {
            return true;
        }

        return await _db.Questions.AnyAsync(x => x.CompanyId == companyId, cancellationToken);
    }

    public Task<List<EmployeeResultDto>> SearchResultsByNrpAsync(AccessScopeDto scope, string? nrp, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(new List<EmployeeResultDto>());
        }

        var query = _db.ExamResults
            .Include(x => x.Session)
                .ThenInclude(s => s!.Employee)
            .Include(x => x.Session)
                .ThenInclude(s => s!.Vehicle)
            .AsQueryable();

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.Session!.Employee!.CompanyId == scope.CompanyId!.Value);
        }

        if (!string.IsNullOrWhiteSpace(nrp))
        {
            query = query.Where(x => x.Session!.Employee!.Nrp.Contains(nrp));
        }

        return query
            .OrderByDescending(x => x.FinishedAt)
            .Select(x => new EmployeeResultDto
            {
                SessionId = x.SessionId,
                VehicleId = x.Session!.VehicleId,
                Nrp = x.Session!.Employee!.Nrp,
                EmployeeName = x.Session!.Employee!.EmployeeName,
                VehicleName = x.Session!.Vehicle!.VehicleName,
                Score = x.Score,
                PassStatus = x.PassStatus,
                FinishedAt = x.FinishedAt
            })
            .ToListAsync(cancellationToken);
    }

    public Task<List<Employee>> GetEmployeesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        return GetEmployeesCoreAsync(scope, cancellationToken);
    }

    public Task<int> CountSimperApplicantsAsync(AccessScopeDto scope, string? searchTerm, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(0);
        }

        return BuildSimperApplicantsQuery(scope, searchTerm).CountAsync(cancellationToken);
    }

    public Task<List<SimperApplicantView>> GetSimperApplicantsAsync(AccessScopeDto scope, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(new List<SimperApplicantView>());
        }

        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize <= 0 ? 50 : pageSize;

        return BuildSimperApplicantsQuery(scope, searchTerm)
            .OrderByDescending(x => x.Id)
            .ThenBy(x => x.Nama)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Vehicle>> GetVehiclesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(new List<Vehicle>());
        }

        var query = _db.Vehicles.AsNoTracking().AsQueryable();
        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.CompanyId == scope.CompanyId!.Value);
        }

        return query.OrderBy(x => x.VehicleName).ToListAsync(cancellationToken);
    }

    public Task<List<Question>> GetQuestionsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(new List<Question>());
        }

        var query = _db.Questions.AsNoTracking().AsQueryable();
        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.CompanyId == scope.CompanyId!.Value);
        }

        return query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<Question?> GetQuestionByIdAsync(long questionId, CancellationToken cancellationToken = default)
    {
        return _db.Questions.FirstOrDefaultAsync(x => x.Id == questionId, cancellationToken);
    }

    public async Task AddEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await _db.Employees.AddAsync(employee, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        await _db.Vehicles.AddAsync(vehicle, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddQuestionAsync(Question question, CancellationToken cancellationToken = default)
    {
        await _db.Questions.AddAsync(question, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateQuestionAsync(Question question, CancellationToken cancellationToken = default)
    {
        _db.Questions.Update(question);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteQuestionAsync(Question question, CancellationToken cancellationToken = default)
    {
        _db.Questions.Remove(question);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<UserLogin?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim().ToLowerInvariant();
        return _db.UserLogins
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Username.ToLower() == normalized && x.IsActive, cancellationToken);
    }

    public Task<UserLogin?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return _db.UserLogins
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<ExamSession?> GetExamSessionByIdAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        return _db.ExamSessions
            .Include(x => x.Employee)
                .ThenInclude(x => x!.Company)
            .Include(x => x.Vehicle)
            .Include(x => x.ExamResult)
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
    }

    public async Task AddUserAsync(UserLogin userLogin, CancellationToken cancellationToken = default)
    {
        await _db.UserLogins.AddAsync(userLogin, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUserAsync(UserLogin userLogin, CancellationToken cancellationToken = default)
    {
        _db.UserLogins.Update(userLogin);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountActiveUsersByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        return _db.UserLogins.CountAsync(x => x.IsActive && x.Role == role, cancellationToken);
    }

    public Task<List<UserLogin>> GetUsersAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(new List<UserLogin>());
        }

        var query = _db.UserLogins
            .Include(x => x.Company)
            .Where(x => x.IsActive)
            .AsQueryable();

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.CompanyId == scope.CompanyId!.Value);
        }

        return query.OrderBy(x => x.Username).ToListAsync(cancellationToken);
    }

    public async Task<AnswerReviewDto?> GetAnswerReviewAsync(AccessScopeDto scope, long sessionId, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return null;
        }

        var sessionQuery = _db.ExamSessions
            .Include(x => x.Employee)
            .Include(x => x.Vehicle)
            .Include(x => x.ExamResult)
            .Where(x => x.Id == sessionId)
            .AsQueryable();

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            sessionQuery = sessionQuery.Where(x => x.Employee!.CompanyId == scope.CompanyId!.Value);
        }

        var session = await sessionQuery.FirstOrDefaultAsync(cancellationToken);
        if (session is null)
        {
            return null;
        }

        var items = await _db.ExamQuestions
            .Include(x => x.Question)
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.QuestionOrder)
            .Select(x => new AnswerReviewItemDto
            {
                Order = x.QuestionOrder,
                QuestionText = x.Question!.QuestionText,
                CorrectAnswer = x.Question.CorrectAnswer,
                SelectedAnswer = _db.ExamAnswers.Where(a => a.SessionId == sessionId && a.QuestionId == x.QuestionId).Select(a => a.SelectedAnswer).FirstOrDefault(),
                IsCorrect = _db.ExamAnswers.Where(a => a.SessionId == sessionId && a.QuestionId == x.QuestionId).Select(a => a.IsCorrect).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new AnswerReviewDto
        {
            SessionId = session.Id,
            Nrp = session.Employee!.Nrp,
            EmployeeName = session.Employee.EmployeeName,
            VehicleName = session.Vehicle!.VehicleName,
            Score = session.ExamResult?.Score ?? 0,
            Items = items
        };
    }

    public async Task SyncEmployeesFromApplicantsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return;
        }

        var applicants = await GetDistinctScheduleApplicantsAsync(scope, cancellationToken);
        if (applicants.Count == 0)
        {
            return;
        }

        foreach (var applicant in applicants)
        {
            var companyId = applicant.CreatedCompanyId!.Value;
            var nik = applicant.Nik!.Trim();
            var name = applicant.Nama?.Trim() ?? string.Empty;
            var employeeName = string.IsNullOrWhiteSpace(name) ? nik : name;

            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO public.tbl_m_employee (nrp, employee_name, company_id, created_at)
                VALUES ({nik}, {employeeName}, {companyId}, {DateTime.UtcNow})
                ON CONFLICT (nrp)
                DO UPDATE SET
                    employee_name = EXCLUDED.employee_name,
                    company_id = EXCLUDED.company_id;", cancellationToken);
        }
    }

    public async Task<List<ScheduleEmployeeDto>> GetScheduleEmployeesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return new List<ScheduleEmployeeDto>();
        }

        await SyncEmployeesFromApplicantsAsync(scope, cancellationToken);

        var applicants = await GetDistinctScheduleApplicantsAsync(scope, cancellationToken);
        if (applicants.Count == 0)
        {
            return new List<ScheduleEmployeeDto>();
        }

        var nrps = applicants
            .Select(x => x.Nik!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var employees = await _db.Employees
            .AsNoTracking()
            .Include(x => x.Company)
            .Where(x => nrps.Contains(x.Nrp))
            .ToListAsync(cancellationToken);

        var employeeByKey = employees.ToDictionary(
            x => BuildEmployeeKey(x.Nrp),
            StringComparer.OrdinalIgnoreCase);

        return applicants
            .Select(applicant =>
            {
                var companyId = applicant.CreatedCompanyId!.Value;
                var nik = applicant.Nik!.Trim();
                var key = BuildEmployeeKey(nik);
                employeeByKey.TryGetValue(key, out var employee);

                return new ScheduleEmployeeDto
                {
                    Id = employee?.Id ?? 0,
                    CompanyId = companyId,
                    CompanyName = applicant.Perusahaan?.Trim() ?? employee?.Company?.CompanyName ?? $"Company {companyId}",
                    Nik = nik,
                    Ktp = applicant.Ktp?.Trim() ?? string.Empty,
                    Nrp = employee?.Nrp ?? nik,
                    EmployeeName = applicant.Nama?.Trim() ?? employee?.EmployeeName ?? nik
                };
            })
            .Where(x => x.Id > 0)
            .OrderBy(x => x.Nik)
            .ThenBy(x => x.EmployeeName)
            .ToList();
    }

    public Task<List<ScheduleVehicleDto>> GetScheduleVehiclesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(new List<ScheduleVehicleDto>());
        }

        var query = _db.Vehicles.AsNoTracking().AsQueryable();
        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.CompanyId == scope.CompanyId!.Value);
        }

        return query
            .OrderBy(x => x.VehicleName)
            .Select(x => new ScheduleVehicleDto
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.CompanyName : $"Company {x.CompanyId}",
                VehicleName = x.VehicleName,
                SimperType = x.SimperType
            })
            .ToListAsync(cancellationToken);
    }

    public Task<List<ScheduleItemDto>> GetScheduleItemsByDateAsync(AccessScopeDto scope, DateTime date, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(new List<ScheduleItemDto>());
        }

        var from = DateTime.SpecifyKind(date.Date, DateTimeKind.Local).ToUniversalTime();
        var to = DateTime.SpecifyKind(date.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();

        var query = _db.ExamSchedules
            .Include(x => x.Employee)
            .Include(x => x.Vehicle)
            .Where(x => x.ScheduledAt >= from && x.ScheduledAt < to)
            .AsQueryable();

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.Employee!.CompanyId == scope.CompanyId!.Value);
        }

        return query
            .OrderBy(x => x.ScheduledAt)
            .Select(x => new ScheduleItemDto
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                CompanyId = x.Employee!.CompanyId,
                CompanyName = x.Employee.Company != null ? x.Employee.Company.CompanyName : $"Company {x.Employee.CompanyId}",
                VehicleId = x.VehicleId,
                ScheduledAt = x.ScheduledAt,
                EmployeeName = x.Employee!.EmployeeName,
                Nrp = x.Employee.Nrp,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ScheduleSlotExistsAsync(long vehicleId, DateTime scheduledAtUtc, long? excludeScheduleId = null, CancellationToken cancellationToken = default)
    {
        return _db.ExamSchedules.AnyAsync(x => x.VehicleId == vehicleId && x.ScheduledAt == scheduledAtUtc && (!excludeScheduleId.HasValue || x.Id != excludeScheduleId.Value), cancellationToken);
    }

    public Task<int> GetScheduleParticipantCountAsync(long vehicleId, DateTime scheduledAtUtc, long? excludeScheduleId = null, CancellationToken cancellationToken = default)
    {
        return _db.ExamSchedules.CountAsync(x =>
            x.VehicleId == vehicleId &&
            x.ScheduledAt == scheduledAtUtc &&
            (!excludeScheduleId.HasValue || x.Id != excludeScheduleId.Value), cancellationToken);
    }

    public Task<bool> ScheduleAssignmentExistsAsync(long employeeId, long vehicleId, DateTime scheduledAtUtc, long? excludeScheduleId = null, CancellationToken cancellationToken = default)
    {
        return _db.ExamSchedules.AnyAsync(x =>
            x.EmployeeId == employeeId &&
            x.VehicleId == vehicleId &&
            x.ScheduledAt == scheduledAtUtc &&
            (!excludeScheduleId.HasValue || x.Id != excludeScheduleId.Value), cancellationToken);
    }

    public async Task<long> AddScheduleAsync(ExamSchedule schedule, CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.ExamSchedules.AddAsync(schedule, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return schedule.Id;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgres &&
                                           postgres.SqlState == "23505" &&
                                           (postgres.ConstraintName?.Contains("vehicle_id_scheduled_at", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            throw new InvalidOperationException("Database masih menerapkan aturan slot tunggal pada unit dan jam yang sama. Restart aplikasi agar skema sesi multi-peserta diterapkan.", ex);
        }
    }

    public async Task<bool> MoveScheduleAsync(AccessScopeDto scope, long scheduleId, long vehicleId, DateTime scheduledAtUtc, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return false;
        }

        var query = _db.ExamSchedules
            .Include(x => x.Employee)
            .Where(x => x.Id == scheduleId)
            .AsQueryable();

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.Employee!.CompanyId == scope.CompanyId!.Value);
        }

        var entity = await query.FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.VehicleId = vehicleId;
        entity.ScheduledAt = scheduledAtUtc;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveScheduleAsync(AccessScopeDto scope, long scheduleId, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return false;
        }

        var query = _db.ExamSchedules
            .Include(x => x.Employee)
            .Where(x => x.Id == scheduleId)
            .AsQueryable();

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.Employee!.CompanyId == scope.CompanyId!.Value);
        }

        var entity = await query.FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _db.ExamSchedules.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<List<long>> GetEmployeeIdsWithAnyScheduleAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(new List<long>());
        }

        var query = _db.ExamSchedules
            .Include(x => x.Employee)
            .AsQueryable();

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.Employee!.CompanyId == scope.CompanyId!.Value);
        }

        return query.Select(x => x.EmployeeId).Distinct().ToListAsync(cancellationToken);
    }

    public Task<List<UserLogin>> GetInstructorsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(new List<UserLogin>());
        }

        var query = _db.UserLogins
            .Include(x => x.Company)
            .Where(x => x.IsActive && x.Role == SystemUserRole.Instructor)
            .AsQueryable();

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.CompanyId == scope.CompanyId!.Value);
        }

        return query
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<PracticalAssessmentTemplate>> GetPracticalTemplatesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(new List<PracticalAssessmentTemplate>());
        }

        var query = _db.PracticalAssessmentTemplates
            .Include(x => x.Company)
            .Include(x => x.Vehicle)
            .Include(x => x.Items)
            .Include(x => x.Sessions)
            .AsQueryable();

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.CompanyId == scope.CompanyId!.Value);
        }

        return query
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Company!.CompanyName)
            .ThenBy(x => x.Vehicle!.VehicleName)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<PracticalAssessmentTemplate?> GetPracticalTemplateByIdAsync(long templateId, CancellationToken cancellationToken = default)
    {
        return _db.PracticalAssessmentTemplates
            .Include(x => x.Items)
            .Include(x => x.Company)
            .Include(x => x.Vehicle)
            .Include(x => x.Sessions)
            .FirstOrDefaultAsync(x => x.Id == templateId, cancellationToken);
    }

    public async Task<long> AddPracticalTemplateAsync(PracticalAssessmentTemplate template, CancellationToken cancellationToken = default)
    {
        await _db.PracticalAssessmentTemplates.AddAsync(template, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return template.Id;
    }

    public async Task AddPracticalTemplateItemsAsync(IEnumerable<PracticalAssessmentTemplateItem> items, CancellationToken cancellationToken = default)
    {
        await _db.PracticalAssessmentTemplateItems.AddRangeAsync(items, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdatePracticalTemplateAsync(PracticalAssessmentTemplate template, CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplacePracticalTemplateItemsAsync(long templateId, IEnumerable<PracticalAssessmentTemplateItem> items, CancellationToken cancellationToken = default)
    {
        var existingItems = await _db.PracticalAssessmentTemplateItems
            .Where(x => x.TemplateId == templateId)
            .ToListAsync(cancellationToken);

        if (existingItems.Count > 0)
        {
            _db.PracticalAssessmentTemplateItems.RemoveRange(existingItems);
        }

        await _db.PracticalAssessmentTemplateItems.AddRangeAsync(items, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePracticalTemplateAsync(PracticalAssessmentTemplate template, CancellationToken cancellationToken = default)
    {
        var items = await _db.PracticalAssessmentTemplateItems
            .Where(x => x.TemplateId == template.Id)
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            _db.PracticalAssessmentTemplateItems.RemoveRange(items);
        }

        _db.PracticalAssessmentTemplates.Remove(template);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<List<PracticalAssessmentSession>> GetPracticalSessionsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(new List<PracticalAssessmentSession>());
        }

        var query = _db.PracticalAssessmentSessions
            .Include(x => x.Employee)
            .ThenInclude(x => x!.Company)
            .Include(x => x.Vehicle)
            .Include(x => x.Template)
            .Include(x => x.InstructorUser)
            .AsQueryable();

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.Employee!.CompanyId == scope.CompanyId!.Value);
        }

        return query
            .OrderByDescending(x => x.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> AddPracticalSessionAsync(PracticalAssessmentSession session, CancellationToken cancellationToken = default)
    {
        await _db.PracticalAssessmentSessions.AddAsync(session, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return session.Id;
    }

    public Task<PracticalAssessmentSession?> GetPracticalSessionByIdAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        return _db.PracticalAssessmentSessions
            .Include(x => x.Employee)
            .ThenInclude(x => x!.Company)
            .Include(x => x.Vehicle)
            .Include(x => x.Template)
            .ThenInclude(x => x!.Items)
            .Include(x => x.InstructorUser)
            .Include(x => x.Scores)
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
    }

    public Task<List<PracticalAssessmentSession>> GetInstructorPracticalSessionsAsync(long instructorUserId, CancellationToken cancellationToken = default)
    {
        return _db.PracticalAssessmentSessions
            .Include(x => x.Employee)
            .ThenInclude(x => x!.Company)
            .Include(x => x.Vehicle)
            .Include(x => x.Template)
            .Include(x => x.InstructorUser)
            .Where(x => x.InstructorUserId == instructorUserId)
            .OrderBy(x => x.Status == PracticalAssessmentStatus.Submitted)
            .ThenByDescending(x => x.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<PracticalAssessmentScore>> GetPracticalScoresAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        return _db.PracticalAssessmentScores
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertPracticalScoresAsync(long sessionId, IEnumerable<PracticalAssessmentScore> scores, CancellationToken cancellationToken = default)
    {
        var incoming = scores.ToList();
        var itemIds = incoming.Select(x => x.TemplateItemId).Distinct().ToList();
        var existing = await _db.PracticalAssessmentScores
            .Where(x => x.SessionId == sessionId && itemIds.Contains(x.TemplateItemId))
            .ToListAsync(cancellationToken);

        foreach (var score in incoming)
        {
            var tracked = existing.FirstOrDefault(x => x.TemplateItemId == score.TemplateItemId);
            if (tracked is null)
            {
                await _db.PracticalAssessmentScores.AddAsync(score, cancellationToken);
                continue;
            }

            tracked.NumericValue = score.NumericValue;
            tracked.GradeValue = score.GradeValue;
            tracked.Note = score.Note;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePracticalSessionAsync(PracticalAssessmentSession session, CancellationToken cancellationToken = default)
    {
        _db.PracticalAssessmentSessions.Update(session);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> MovePracticalSessionAsync(AccessScopeDto scope, long sessionId, long vehicleId, DateTime scheduledAtUtc, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return false;
        }

        var query = _db.PracticalAssessmentSessions
            .Include(x => x.Employee)
            .Where(x => x.Id == sessionId)
            .AsQueryable();

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.Employee!.CompanyId == scope.CompanyId!.Value);
        }

        var entity = await query.FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.VehicleId = vehicleId;
        entity.ScheduledAt = scheduledAtUtc;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemovePracticalSessionAsync(AccessScopeDto scope, long sessionId, CancellationToken cancellationToken = default)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return false;
        }

        var query = _db.PracticalAssessmentSessions
            .Include(x => x.Employee)
            .Where(x => x.Id == sessionId)
            .AsQueryable();

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.Employee!.CompanyId == scope.CompanyId!.Value);
        }

        var entity = await query.FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _db.PracticalAssessmentSessions.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<SimperApplicantView?> GetLatestApplicantByNikAsync(string nik, CancellationToken cancellationToken = default)
    {
        var normalizedNik = nik.Trim();
        return _db.SimperApplicants
            .AsNoTracking()
            .Where(x => x.Nik != null && x.Nik == normalizedNik)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool IsInvalidCompanyScope(AccessScopeDto scope)
    {
        return !scope.IsAdministrator && !scope.HasCompanyScope;
    }

    private Task<List<Employee>> GetEmployeesCoreAsync(AccessScopeDto scope, CancellationToken cancellationToken)
    {
        if (IsInvalidCompanyScope(scope))
        {
            return Task.FromResult(new List<Employee>());
        }

        var query = _db.Employees
            .AsNoTracking()
            .Include(x => x.Company)
            .AsQueryable();

        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.CompanyId == scope.CompanyId!.Value);
        }

        return query
            .OrderBy(x => x.Nrp)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<SimperApplicantView>> GetDistinctScheduleApplicantsAsync(AccessScopeDto scope, CancellationToken cancellationToken)
    {
        var applicants = await BuildSimperApplicantsQuery(scope, null)
            .Where(x => x.CreatedCompanyId.HasValue && x.Nik != null && x.Nik != "")
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return applicants
            .GroupBy(x => BuildEmployeeKey(x.Nik!))
            .Select(group => group.First())
            .ToList();
    }

    private static string BuildEmployeeKey(string nrpOrNik)
    {
        return nrpOrNik.Trim().ToUpperInvariant();
    }

    private IQueryable<SimperApplicantView> BuildSimperApplicantsQuery(AccessScopeDto scope, string? searchTerm)
    {
        var query = _db.SimperApplicants.AsNoTracking().AsQueryable();
        if (!scope.IsAdministrator && scope.HasCompanyScope)
        {
            query = query.Where(x => x.CreatedCompanyId == scope.CompanyId!.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm.Trim()}%";
            query = query.Where(x =>
                (x.Nama != null && EF.Functions.ILike(x.Nama, pattern)) ||
                (x.Nik != null && EF.Functions.ILike(x.Nik, pattern)) ||
                (x.Ktp != null && EF.Functions.ILike(x.Ktp, pattern)) ||
                (x.Nomor != null && EF.Functions.ILike(x.Nomor, pattern)) ||
                (x.Pengajuan != null && EF.Functions.ILike(x.Pengajuan, pattern)) ||
                (x.Perusahaan != null && EF.Functions.ILike(x.Perusahaan, pattern)) ||
                (x.Departemen != null && EF.Functions.ILike(x.Departemen, pattern)));
        }

        return query;
    }
}
