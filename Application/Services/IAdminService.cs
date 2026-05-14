using SimperSecureOnlineTestSystem.Application.DTOs;
using SimperSecureOnlineTestSystem.Domain.Entities;

namespace SimperSecureOnlineTestSystem.Application.Services;

public interface IAdminService
{
    Task<List<AdminExamMonitorDto>> GetActiveExamMonitorsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<List<EmployeeResultDto>> SearchResultsByNrpAsync(AccessScopeDto scope, string? nrp, CancellationToken cancellationToken = default);
    Task<byte[]> ExportResultsCsvAsync(AccessScopeDto scope, string? nrp, CancellationToken cancellationToken = default);

    Task<List<Employee>> GetEmployeesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<int> CountSimperApplicantsAsync(AccessScopeDto scope, string? searchTerm, CancellationToken cancellationToken = default);
    Task<List<SimperApplicantView>> GetSimperApplicantsAsync(AccessScopeDto scope, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<Vehicle>> GetVehiclesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<List<Question>> GetQuestionsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<Question?> GetQuestionByIdAsync(long questionId, CancellationToken cancellationToken = default);
    Task<List<Company>> GetCompaniesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task AddEmployeeAsync(Employee employee, CancellationToken cancellationToken = default);
    Task AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task AddQuestionAsync(Question question, CancellationToken cancellationToken = default);
    Task UpdateQuestionAsync(Question question, CancellationToken cancellationToken = default);
    Task DeleteQuestionAsync(Question question, CancellationToken cancellationToken = default);
    Task AddCompanyAsync(CompanyUpsertDto company, CancellationToken cancellationToken = default);
    Task UpdateCompanyAsync(CompanyUpsertDto company, CancellationToken cancellationToken = default);
    Task DeleteCompanyAsync(long companyId, CancellationToken cancellationToken = default);

    Task<AuthUserDto?> ValidateUserAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<UserLogin?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<List<UserLogin>> GetUsersAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task AddUserAsync(CreateUserDto user, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(UpdateUserDto user, CancellationToken cancellationToken = default);
    Task ResetUserPasswordAsync(ResetUserPasswordDto user, long? actorUserId = null, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(long userId, long? actorUserId = null, CancellationToken cancellationToken = default);
    Task ChangeOwnPasswordAsync(ProfilePasswordDto user, CancellationToken cancellationToken = default);
    Task<AnswerReviewDto?> GetAnswerReviewAsync(AccessScopeDto scope, long sessionId, CancellationToken cancellationToken = default);
    Task<ScheduleBoardDto> GetScheduleBoardAsync(AccessScopeDto scope, DateTime date, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, long? ScheduleId)> AssignScheduleAsync(AccessScopeDto scope, ScheduleAssignRequestDto request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> MoveScheduleAsync(AccessScopeDto scope, ScheduleMoveRequestDto request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> RemoveScheduleAsync(AccessScopeDto scope, long scheduleId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, int ScheduledCount)> AutoScheduleAsync(AccessScopeDto scope, AutoScheduleRequestDto request, CancellationToken cancellationToken = default);
    Task<List<UserLogin>> GetInstructorsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task<List<PracticalTemplateSummaryDto>> GetPracticalTemplatesAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task CreatePracticalTemplateAsync(AccessScopeDto scope, PracticalTemplateUpsertDto request, CancellationToken cancellationToken = default);
    Task UpdatePracticalTemplateAsync(AccessScopeDto scope, long templateId, PracticalTemplateUpsertDto request, CancellationToken cancellationToken = default);
    Task SetPracticalTemplateStatusAsync(AccessScopeDto scope, long templateId, bool isActive, CancellationToken cancellationToken = default);
    Task DeletePracticalTemplateAsync(AccessScopeDto scope, long templateId, CancellationToken cancellationToken = default);
    Task<List<PracticalSessionSummaryDto>> GetPracticalSessionsAsync(AccessScopeDto scope, CancellationToken cancellationToken = default);
    Task CreatePracticalSessionAsync(AccessScopeDto scope, PracticalSessionCreateDto request, CancellationToken cancellationToken = default);
    Task<List<PracticalSessionSummaryDto>> GetInstructorPracticalSessionsAsync(long instructorUserId, CancellationToken cancellationToken = default);
    Task<PracticalEvaluationDto?> GetPracticalEvaluationAsync(AccessScopeDto scope, long sessionId, long? currentUserId, bool isInstructor, CancellationToken cancellationToken = default);
    Task SubmitPracticalEvaluationAsync(AccessScopeDto scope, PracticalEvaluationSubmitDto request, long? currentUserId, bool isInstructor, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> MovePracticalSessionAsync(AccessScopeDto scope, PracticalSessionMoveDto request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> RemovePracticalSessionAsync(AccessScopeDto scope, long sessionId, CancellationToken cancellationToken = default);
    Task<SummaryScoreDto?> GetSummaryScoreAsync(AccessScopeDto scope, long theorySessionId, long practicalSessionId, long? currentUserId, bool isInstructor, CancellationToken cancellationToken = default);
}
