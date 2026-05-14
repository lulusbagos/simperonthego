using SimperSecureOnlineTestSystem.Application.DTOs;
using SimperSecureOnlineTestSystem.Domain.Entities;

namespace SimperSecureOnlineTestSystem.Application.Services;

public interface IExamService
{
    Task<Employee?> VerifyEmployeeAsync(string nrp, CancellationToken cancellationToken = default);
    Task<List<Vehicle>> GetVehiclesByNrpAsync(string nrp, CancellationToken cancellationToken = default);
    Task<SessionCreateResponseDto> CreateSessionAsync(SessionCreateRequestDto request, CancellationToken cancellationToken = default);
    Task<string?> ResolveTokenByRefIdAsync(string refId, CancellationToken cancellationToken = default);
    Task<bool> ValidateSessionAccessPasswordAsync(string token, string password, CancellationToken cancellationToken = default);
    Task<SessionValidationDto> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> SessionHasQuestionsAsync(string token, CancellationToken cancellationToken = default);
    Task StartSessionAsync(string token, CancellationToken cancellationToken = default);
    Task<ExamQuestionDto?> GetQuestionAsync(string token, int order, CancellationToken cancellationToken = default);
    Task<SaveAnswerResponseDto> SaveAnswerAsync(SaveAnswerRequestDto request, CancellationToken cancellationToken = default);
    Task<ExamResultDto> SubmitSessionAsync(string token, CancellationToken cancellationToken = default);
    Task UpdateCameraStatusAsync(CameraStatusDto request, CancellationToken cancellationToken = default);
    Task LogEventAsync(LogEventDto request, CancellationToken cancellationToken = default);
}
