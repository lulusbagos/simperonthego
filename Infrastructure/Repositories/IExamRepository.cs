using SimperSecureOnlineTestSystem.Application.DTOs;
using SimperSecureOnlineTestSystem.Domain.Entities;
using SimperSecureOnlineTestSystem.Domain.Enums;

namespace SimperSecureOnlineTestSystem.Infrastructure.Repositories;

public interface IExamRepository
{
    Task<Employee?> GetEmployeeByNrpAsync(string nrp, CancellationToken cancellationToken = default);
    Task<List<Vehicle>> GetVehiclesByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task<Vehicle?> GetVehicleByIdAsync(long vehicleId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveSessionAsync(long employeeId, long vehicleId, CancellationToken cancellationToken = default);
    Task<bool> RefIdExistsAsync(string refId, CancellationToken cancellationToken = default);
    Task<ExamSession> AddSessionAsync(ExamSession session, CancellationToken cancellationToken = default);
    Task<ExamSession?> GetSessionByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<ExamSession?> GetSessionByRefIdAsync(string refId, CancellationToken cancellationToken = default);
    Task<List<Question>> GetRandomQuestionsAsync(long companyId, long vehicleId, int take, CancellationToken cancellationToken = default);
    Task AddExamQuestionsAsync(IEnumerable<ExamQuestion> examQuestions, CancellationToken cancellationToken = default);
    Task<ExamQuestion?> GetSessionQuestionAsync(long sessionId, int order, CancellationToken cancellationToken = default);
    Task<int> GetTotalQuestionCountAsync(long sessionId, CancellationToken cancellationToken = default);
    Task<ExamAnswer?> GetAnswerAsync(long sessionId, long questionId, CancellationToken cancellationToken = default);
    Task UpsertAnswerAsync(ExamAnswer answer, CancellationToken cancellationToken = default);
    Task<int> GetAnsweredCountAsync(long sessionId, CancellationToken cancellationToken = default);
    Task SetSessionStatusAsync(long sessionId, ExamSessionStatus status, CancellationToken cancellationToken = default);
    Task AddResultAsync(ExamResult result, CancellationToken cancellationToken = default);
    Task<ExamResult?> GetResultBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default);
    Task AddLogAsync(ExamLog log, CancellationToken cancellationToken = default);
    Task SyncScheduleStatusAsync(long employeeId, long vehicleId, string status, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
