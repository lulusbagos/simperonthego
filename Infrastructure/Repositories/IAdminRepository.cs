using SimperSecureOnlineTestSystem.Application.DTOs;
using SimperSecureOnlineTestSystem.Domain.Entities;

namespace SimperSecureOnlineTestSystem.Infrastructure.Repositories;

public interface IAdminRepository
{
    Task<List<AdminExamMonitorDto>> GetActiveExamMonitorsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<List<EmployeeResultDto>> SearchResultsByNrpAsync(AccessScopeDto scope, string? nrp, CancellationToken cancellationToken = default);
    Task<List<Company>> GetCompaniesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<Company?> GetCompanyByIdAsync(long companyId, CancellationToken cancellationToken = default);
    Task<Company?> GetCompanyByNameAsync(string companyName, CancellationToken cancellationToken = default);
    Task AddCompanyAsync(Company company, CancellationToken cancellationToken = default);
    Task UpdateCompanyAsync(Company company, CancellationToken cancellationToken = default);
    Task DeleteCompanyAsync(Company company, CancellationToken cancellationToken = default);
    Task<bool> CompanyHasDependenciesAsync(long companyId, CancellationToken cancellationToken = default);
    Task<List<Employee>> GetEmployeesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<int> CountSimperApplicantsAsync(AccessScopeDto scope, string? searchTerm, CancellationToken cancellationToken = default);
    Task<List<SimperApplicantView>> GetSimperApplicantsAsync(AccessScopeDto scope, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<Vehicle>> GetVehiclesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<List<Question>> GetQuestionsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<Question?> GetQuestionByIdAsync(long questionId, CancellationToken cancellationToken = default);
    Task AddEmployeeAsync(Employee employee, CancellationToken cancellationToken = default);
    Task AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task AddQuestionAsync(Question question, CancellationToken cancellationToken = default);
    Task UpdateQuestionAsync(Question question, CancellationToken cancellationToken = default);
    Task DeleteQuestionAsync(Question question, CancellationToken cancellationToken = default);

    Task<UserLogin?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<UserLogin?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<ExamSession?> GetExamSessionByIdAsync(long sessionId, CancellationToken cancellationToken = default);
    Task AddUserAsync(UserLogin userLogin, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(UserLogin userLogin, CancellationToken cancellationToken = default);
    Task<int> CountActiveUsersByRoleAsync(string role, CancellationToken cancellationToken = default);
    Task<List<UserLogin>> GetUsersAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<AnswerReviewDto?> GetAnswerReviewAsync(AccessScopeDto scope, long sessionId, CancellationToken cancellationToken = default);
    Task SyncEmployeesFromApplicantsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<List<ScheduleEmployeeDto>> GetScheduleEmployeesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<List<ScheduleVehicleDto>> GetScheduleVehiclesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<List<ScheduleItemDto>> GetScheduleItemsByDateAsync(AccessScopeDto scope, DateTime date, CancellationToken cancellationToken = default);
    Task<bool> ScheduleSlotExistsAsync(long vehicleId, DateTime scheduledAtUtc, long? excludeScheduleId = null, CancellationToken cancellationToken = default);
    Task<int> GetScheduleParticipantCountAsync(long vehicleId, DateTime scheduledAtUtc, long? excludeScheduleId = null, CancellationToken cancellationToken = default);
    Task<bool> ScheduleAssignmentExistsAsync(long employeeId, long vehicleId, DateTime scheduledAtUtc, long? excludeScheduleId = null, CancellationToken cancellationToken = default);
    Task<long> AddScheduleAsync(ExamSchedule schedule, CancellationToken cancellationToken = default);
    Task<bool> MoveScheduleAsync(AccessScopeDto scope, long scheduleId, long vehicleId, DateTime scheduledAtUtc, CancellationToken cancellationToken = default);
    Task<bool> RemoveScheduleAsync(AccessScopeDto scope, long scheduleId, CancellationToken cancellationToken = default);
    Task<List<long>> GetEmployeeIdsWithAnyScheduleAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<List<UserLogin>> GetInstructorsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<List<PracticalAssessmentTemplate>> GetPracticalTemplatesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<PracticalAssessmentTemplate?> GetPracticalTemplateByIdAsync(long templateId, CancellationToken cancellationToken = default);
    Task<long> AddPracticalTemplateAsync(PracticalAssessmentTemplate template, CancellationToken cancellationToken = default);
    Task AddPracticalTemplateItemsAsync(IEnumerable<PracticalAssessmentTemplateItem> items, CancellationToken cancellationToken = default);
    Task UpdatePracticalTemplateAsync(PracticalAssessmentTemplate template, CancellationToken cancellationToken = default);
    Task ReplacePracticalTemplateItemsAsync(long templateId, IEnumerable<PracticalAssessmentTemplateItem> items, CancellationToken cancellationToken = default);
    Task DeletePracticalTemplateAsync(PracticalAssessmentTemplate template, CancellationToken cancellationToken = default);
    Task<List<PracticalAssessmentSession>> GetPracticalSessionsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<long> AddPracticalSessionAsync(PracticalAssessmentSession session, CancellationToken cancellationToken = default);
    Task<PracticalAssessmentSession?> GetPracticalSessionByIdAsync(long sessionId, CancellationToken cancellationToken = default);
    Task<List<PracticalAssessmentSession>> GetInstructorPracticalSessionsAsync(long instructorUserId, CancellationToken cancellationToken = default);
    Task<List<PracticalAssessmentScore>> GetPracticalScoresAsync(long sessionId, CancellationToken cancellationToken = default);
    Task UpsertPracticalScoresAsync(long sessionId, IEnumerable<PracticalAssessmentScore> scores, CancellationToken cancellationToken = default);
    Task UpdatePracticalSessionAsync(PracticalAssessmentSession session, CancellationToken cancellationToken = default);
    Task<bool> MovePracticalSessionAsync(AccessScopeDto scope, long sessionId, long vehicleId, DateTime scheduledAtUtc, CancellationToken cancellationToken = default);
    Task<bool> RemovePracticalSessionAsync(AccessScopeDto scope, long sessionId, CancellationToken cancellationToken = default);
    Task<SimperApplicantView?> GetLatestApplicantByNikAsync(string nik, CancellationToken cancellationToken = default);
}
