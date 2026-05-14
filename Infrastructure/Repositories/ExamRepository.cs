using Microsoft.EntityFrameworkCore;
using SimperSecureOnlineTestSystem.Domain.Entities;
using SimperSecureOnlineTestSystem.Domain.Enums;
using SimperSecureOnlineTestSystem.Infrastructure.Data;

namespace SimperSecureOnlineTestSystem.Infrastructure.Repositories;

public class ExamRepository : IExamRepository
{
    private readonly ApplicationDbContext _db;

    public ExamRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Employee?> GetEmployeeByNrpAsync(string nrp, CancellationToken cancellationToken = default)
    {
        var normalizedNrp = nrp.Trim();
        return _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Nrp == normalizedNrp, cancellationToken);
    }

    public Task<List<Vehicle>> GetVehiclesByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
    {
        return _db.Vehicles
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.VehicleName)
            .ToListAsync(cancellationToken);
    }

    public Task<Vehicle?> GetVehicleByIdAsync(long vehicleId, CancellationToken cancellationToken = default)
    {
        return _db.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == vehicleId, cancellationToken);
    }

    public Task<bool> HasActiveSessionAsync(long employeeId, long vehicleId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return _db.ExamSessions
            .AnyAsync(x => x.EmployeeId == employeeId && x.VehicleId == vehicleId && (x.Status == ExamSessionStatus.Pending || x.Status == ExamSessionStatus.Active) && x.EndTime > now, cancellationToken);
    }

    public Task<bool> RefIdExistsAsync(string refId, CancellationToken cancellationToken = default)
    {
        return _db.ExamSessions.AnyAsync(x => x.RefId == refId, cancellationToken);
    }

    public async Task<ExamSession> AddSessionAsync(ExamSession session, CancellationToken cancellationToken = default)
    {
        await _db.ExamSessions.AddAsync(session, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public Task<ExamSession?> GetSessionByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return _db.ExamSessions
            .Include(x => x.Employee)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
    }

    public Task<ExamSession?> GetSessionByRefIdAsync(string refId, CancellationToken cancellationToken = default)
    {
        var normalized = refId.Trim().ToUpperInvariant();
        return _db.ExamSessions
            .Include(x => x.Employee)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.RefId == normalized, cancellationToken);
    }

    public Task<List<Question>> GetRandomQuestionsAsync(long companyId, long vehicleId, int take, CancellationToken cancellationToken = default)
    {
        return _db.Questions
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.VehicleId == vehicleId)
            .OrderBy(_ => EF.Functions.Random())
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddExamQuestionsAsync(IEnumerable<ExamQuestion> examQuestions, CancellationToken cancellationToken = default)
    {
        await _db.ExamQuestions.AddRangeAsync(examQuestions, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<ExamQuestion?> GetSessionQuestionAsync(long sessionId, int order, CancellationToken cancellationToken = default)
    {
        return _db.ExamQuestions
            .Include(x => x.Question)
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.QuestionOrder == order, cancellationToken);
    }

    public Task<int> GetTotalQuestionCountAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        return _db.ExamQuestions.CountAsync(x => x.SessionId == sessionId, cancellationToken);
    }

    public Task<ExamAnswer?> GetAnswerAsync(long sessionId, long questionId, CancellationToken cancellationToken = default)
    {
        return _db.ExamAnswers.FirstOrDefaultAsync(x => x.SessionId == sessionId && x.QuestionId == questionId, cancellationToken);
    }

    public async Task UpsertAnswerAsync(ExamAnswer answer, CancellationToken cancellationToken = default)
    {
        var existing = await _db.ExamAnswers.FirstOrDefaultAsync(x => x.SessionId == answer.SessionId && x.QuestionId == answer.QuestionId, cancellationToken);
        if (existing is null)
        {
            await _db.ExamAnswers.AddAsync(answer, cancellationToken);
        }
        else
        {
            existing.SelectedAnswer = answer.SelectedAnswer;
            existing.IsCorrect = answer.IsCorrect;
            existing.AnsweredAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> GetAnsweredCountAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        return _db.ExamAnswers.CountAsync(x => x.SessionId == sessionId && x.SelectedAnswer != null, cancellationToken);
    }

    public async Task SetSessionStatusAsync(long sessionId, ExamSessionStatus status, CancellationToken cancellationToken = default)
    {
        var session = await _db.ExamSessions.FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return;
        }

        session.Status = status;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddResultAsync(ExamResult result, CancellationToken cancellationToken = default)
    {
        await _db.ExamResults.AddAsync(result, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<ExamResult?> GetResultBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        return _db.ExamResults.FirstOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);
    }

    public async Task AddLogAsync(ExamLog log, CancellationToken cancellationToken = default)
    {
        await _db.ExamLogs.AddAsync(log, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SyncScheduleStatusAsync(long employeeId, long vehicleId, string status, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var schedule = await _db.ExamSchedules
            .Where(x => x.EmployeeId == employeeId && x.VehicleId == vehicleId && x.Status != "completed")
            .OrderByDescending(x => x.ScheduledAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (schedule is null)
        {
            return;
        }

        schedule.Status = status;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
