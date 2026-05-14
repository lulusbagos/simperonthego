using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using SimperSecureOnlineTestSystem.Application.DTOs;
using SimperSecureOnlineTestSystem.Domain.Entities;
using SimperSecureOnlineTestSystem.Domain.Enums;
using SimperSecureOnlineTestSystem.Hubs;
using SimperSecureOnlineTestSystem.Infrastructure.Repositories;

namespace SimperSecureOnlineTestSystem.Application.Services;

public class ExamService : IExamService
{
    private const int DefaultQuestionCount = 25;
    private readonly IExamRepository _examRepository;
    private readonly IHubContext<MonitoringHub> _hubContext;
    private readonly PasswordHasher<ExamSession> _passwordHasher = new();

    public ExamService(IExamRepository examRepository, IHubContext<MonitoringHub> hubContext)
    {
        _examRepository = examRepository;
        _hubContext = hubContext;
    }

    public Task<Employee?> VerifyEmployeeAsync(string nrp, CancellationToken cancellationToken = default)
    {
        return _examRepository.GetEmployeeByNrpAsync(nrp, cancellationToken);
    }

    public async Task<List<Vehicle>> GetVehiclesByNrpAsync(string nrp, CancellationToken cancellationToken = default)
    {
        var employee = await _examRepository.GetEmployeeByNrpAsync(nrp, cancellationToken);
        if (employee is null)
        {
            return new List<Vehicle>();
        }

        return await _examRepository.GetVehiclesByCompanyAsync(employee.CompanyId, cancellationToken);
    }

    public async Task<SessionCreateResponseDto> CreateSessionAsync(SessionCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        var employee = await _examRepository.GetEmployeeByNrpAsync(request.Nrp, cancellationToken)
            ?? throw new InvalidOperationException("Employee not found.");

        var hasActive = await _examRepository.HasActiveSessionAsync(employee.Id, request.VehicleId, cancellationToken);
        if (hasActive)
        {
            throw new InvalidOperationException("Employee already has an active exam session for this unit/vehicle.");
        }

        var vehicle = await _examRepository.GetVehicleByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new InvalidOperationException("Vehicle not found.");

        var token = GenerateSecureToken();
        var refId = await GenerateUniqueRefIdAsync(cancellationToken);
        var accessPassword = GenerateAccessPassword();
        var endTime = DateTime.UtcNow.AddMinutes(request.DurationMinutes);

        var questions = await _examRepository.GetRandomQuestionsAsync(vehicle.CompanyId, request.VehicleId, DefaultQuestionCount, cancellationToken);
        if (questions.Count == 0)
        {
            throw new InvalidOperationException("Soal untuk unit ini belum tersedia. Hubungi admin untuk melengkapi bank soal.");
        }

        var session = new ExamSession
        {
            EmployeeId = employee.Id,
            VehicleId = request.VehicleId,
            Token = token,
            RefId = refId,
            AccessPasswordHash = string.Empty,
            EndTime = endTime,
            Status = ExamSessionStatus.Pending,
            CameraActive = false,
            TabSwitchCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        session.AccessPasswordHash = _passwordHasher.HashPassword(session, accessPassword);

        session = await _examRepository.AddSessionAsync(session, cancellationToken);

        var examQuestions = questions
            .Select((question, index) => new ExamQuestion
            {
                SessionId = session.Id,
                QuestionId = question.Id,
                QuestionOrder = index + 1
            })
            .ToList();

        await _examRepository.AddExamQuestionsAsync(examQuestions, cancellationToken);

        await _hubContext.Clients.Group("admins").SendAsync("SessionCreated", new
        {
            sessionId = session.Id,
            employeeName = employee.EmployeeName,
            nrp = employee.Nrp,
            vehicleName = vehicle.VehicleName,
            endTime = session.EndTime
        }, cancellationToken);

        return new SessionCreateResponseDto(session.Id, session.Token, session.RefId, accessPassword, session.EndTime, employee.EmployeeName, vehicle.VehicleName);
    }

    public async Task<string?> ResolveTokenByRefIdAsync(string refId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refId))
        {
            return null;
        }

        var session = await _examRepository.GetSessionByRefIdAsync(refId, cancellationToken);
        return session?.Token;
    }

    public async Task<bool> ValidateSessionAccessPasswordAsync(string token, string password, CancellationToken cancellationToken = default)
    {
        var session = await _examRepository.GetSessionByTokenAsync(token, cancellationToken);
        if (session is null)
        {
            return false;
        }

        var verify = _passwordHasher.VerifyHashedPassword(session, session.AccessPasswordHash, password);
        return verify != PasswordVerificationResult.Failed;
    }

    public async Task<SessionValidationDto> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var session = await _examRepository.GetSessionByTokenAsync(token, cancellationToken);
        if (session is null)
        {
            return new SessionValidationDto(false, "Invalid session token.", 0, DateTime.MinValue, false);
        }

        if (session.EndTime <= DateTime.UtcNow)
        {
            if (session.Status is not ExamSessionStatus.Completed and not ExamSessionStatus.Expired)
            {
                await _examRepository.SetSessionStatusAsync(session.Id, ExamSessionStatus.Expired, cancellationToken);
            }

            return new SessionValidationDto(false, "Session token expired.", session.Id, session.EndTime, session.StartTime.HasValue);
        }

        if (session.Status == ExamSessionStatus.Completed)
        {
            return new SessionValidationDto(false, "Session already completed.", session.Id, session.EndTime, true);
        }

        return new SessionValidationDto(true, "OK", session.Id, session.EndTime, session.StartTime.HasValue);
    }

    public async Task<bool> SessionHasQuestionsAsync(string token, CancellationToken cancellationToken = default)
    {
        var session = await _examRepository.GetSessionByTokenAsync(token, cancellationToken);
        if (session is null)
        {
            return false;
        }

        var totalQuestions = await _examRepository.GetTotalQuestionCountAsync(session.Id, cancellationToken);
        return totalQuestions > 0;
    }

    public async Task StartSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        var session = await _examRepository.GetSessionByTokenAsync(token, cancellationToken)
            ?? throw new InvalidOperationException("Session not found.");

        if (session.StartTime is null)
        {
            session.StartTime = DateTime.UtcNow;
        }

        session.Status = ExamSessionStatus.Active;
        await _examRepository.SaveChangesAsync(cancellationToken);
        await _examRepository.SyncScheduleStatusAsync(session.EmployeeId, session.VehicleId, "in_progress", cancellationToken);

        await _hubContext.Clients.Group("admins").SendAsync("SessionStarted", new { sessionId = session.Id }, cancellationToken);
    }

    public async Task<ExamQuestionDto?> GetQuestionAsync(string token, int order, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateTokenAsync(token, cancellationToken);
        if (!validation.IsValid)
        {
            return null;
        }

        var examQuestion = await _examRepository.GetSessionQuestionAsync(validation.SessionId, order, cancellationToken);
        if (examQuestion?.Question is null)
        {
            return null;
        }

        var totalQuestions = await _examRepository.GetTotalQuestionCountAsync(validation.SessionId, cancellationToken);
        var answer = await _examRepository.GetAnswerAsync(validation.SessionId, examQuestion.QuestionId, cancellationToken);

        var options = new List<OptionDto>
        {
            new("A", examQuestion.Question.OptionA),
            new("B", examQuestion.Question.OptionB),
            new("C", examQuestion.Question.OptionC),
            new("D", examQuestion.Question.OptionD)
        };

        var shuffled = ShuffleDeterministically(options, validation.SessionId, examQuestion.QuestionId);

        return new ExamQuestionDto
        {
            SessionId = validation.SessionId,
            Order = examQuestion.QuestionOrder,
            QuestionId = examQuestion.QuestionId,
            QuestionText = examQuestion.Question.QuestionText,
            ImageUrl = examQuestion.Question.ImageUrl,
            VideoUrl = examQuestion.Question.VideoUrl,
            Options = shuffled,
            SelectedAnswer = answer?.SelectedAnswer,
            TotalQuestions = totalQuestions,
            EndTime = validation.EndTime
        };
    }

    public async Task<SaveAnswerResponseDto> SaveAnswerAsync(SaveAnswerRequestDto request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateTokenAsync(request.Token, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.Message);
        }

        var cameraSession = await _examRepository.GetSessionByTokenAsync(request.Token, cancellationToken)
            ?? throw new InvalidOperationException("Session not found.");

        if (!cameraSession.CameraActive)
        {
            throw new InvalidOperationException("Camera must remain active during exam.");
        }

        var examQuestion = await _examRepository.GetSessionQuestionAsync(validation.SessionId, await GetQuestionOrderByQuestionId(validation.SessionId, request.QuestionId, cancellationToken), cancellationToken);
        if (examQuestion?.Question is null)
        {
            throw new InvalidOperationException("Question not found in session.");
        }

        var isCorrect = string.Equals(examQuestion.Question.CorrectAnswer, request.SelectedAnswer, StringComparison.OrdinalIgnoreCase);

        var answer = new ExamAnswer
        {
            SessionId = validation.SessionId,
            QuestionId = request.QuestionId,
            SelectedAnswer = request.SelectedAnswer.ToUpperInvariant(),
            IsCorrect = isCorrect,
            AnsweredAt = DateTime.UtcNow
        };

        await _examRepository.UpsertAnswerAsync(answer, cancellationToken);

        var answeredCount = await _examRepository.GetAnsweredCountAsync(validation.SessionId, cancellationToken);
        var totalQuestions = await _examRepository.GetTotalQuestionCountAsync(validation.SessionId, cancellationToken);

        await _hubContext.Clients.Group("admins").SendAsync("AnswerSaved", new
        {
            sessionId = validation.SessionId,
            answeredCount,
            totalQuestions
        }, cancellationToken);

        return new SaveAnswerResponseDto(true, answeredCount, totalQuestions);
    }

    public async Task<ExamResultDto> SubmitSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateTokenAsync(token, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.Message);
        }

        var cameraSession = await _examRepository.GetSessionByTokenAsync(token, cancellationToken)
            ?? throw new InvalidOperationException("Session not found.");

        if (!cameraSession.CameraActive)
        {
            throw new InvalidOperationException("Cannot submit while camera is inactive.");
        }

        var existingResult = await _examRepository.GetResultBySessionIdAsync(validation.SessionId, cancellationToken);
        if (existingResult is not null)
        {
            return new ExamResultDto
            {
                SessionId = existingResult.SessionId,
                TotalQuestions = existingResult.TotalQuestions,
                CorrectAnswers = existingResult.CorrectAnswers,
                Score = existingResult.Score,
                PassStatus = existingResult.PassStatus,
                FinishedAt = existingResult.FinishedAt
            };
        }

        var totalQuestions = await _examRepository.GetTotalQuestionCountAsync(validation.SessionId, cancellationToken);
        var answeredCount = await _examRepository.GetAnsweredCountAsync(validation.SessionId, cancellationToken);
        var allAnswersCorrectCount = await CountCorrectAnswers(validation.SessionId, cancellationToken);

        var score = totalQuestions == 0 ? 0 : Math.Round((decimal)allAnswersCorrectCount / totalQuestions * 100, 2);
        var result = new ExamResult
        {
            SessionId = validation.SessionId,
            TotalQuestions = totalQuestions,
            CorrectAnswers = allAnswersCorrectCount,
            Score = score,
            PassStatus = score >= 70,
            FinishedAt = DateTime.UtcNow
        };

        await _examRepository.AddResultAsync(result, cancellationToken);
        await _examRepository.SetSessionStatusAsync(validation.SessionId, ExamSessionStatus.Completed, cancellationToken);
        await _examRepository.SyncScheduleStatusAsync(cameraSession.EmployeeId, cameraSession.VehicleId, "completed", cancellationToken);

        await _hubContext.Clients.Group("admins").SendAsync("SessionCompleted", new
        {
            sessionId = validation.SessionId,
            score,
            answeredCount,
            totalQuestions
        }, cancellationToken);

        return new ExamResultDto
        {
            SessionId = result.SessionId,
            TotalQuestions = result.TotalQuestions,
            CorrectAnswers = result.CorrectAnswers,
            Score = result.Score,
            PassStatus = result.PassStatus,
            FinishedAt = result.FinishedAt
        };
    }

    public async Task UpdateCameraStatusAsync(CameraStatusDto request, CancellationToken cancellationToken = default)
    {
        var session = await _examRepository.GetSessionByTokenAsync(request.Token, cancellationToken)
            ?? throw new InvalidOperationException("Session not found.");

        session.CameraActive = request.IsActive;
        await _examRepository.SaveChangesAsync(cancellationToken);

        if (!request.IsActive)
        {
            await LogEventAsync(new LogEventDto(request.Token, "camera_off", "Camera disabled by participant."), cancellationToken);
        }

        await _hubContext.Clients.Group("admins").SendAsync("CameraStatusChanged", new
        {
            sessionId = session.Id,
            cameraActive = request.IsActive
        }, cancellationToken);
    }

    public async Task LogEventAsync(LogEventDto request, CancellationToken cancellationToken = default)
    {
        var session = await _examRepository.GetSessionByTokenAsync(request.Token, cancellationToken)
            ?? throw new InvalidOperationException("Session not found.");

        if (request.LogType == "tab_switch")
        {
            session.TabSwitchCount += 1;
            await _examRepository.SaveChangesAsync(cancellationToken);
        }

        var log = new ExamLog
        {
            SessionId = session.Id,
            LogType = request.LogType,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        await _examRepository.AddLogAsync(log, cancellationToken);

        await _hubContext.Clients.Group("admins").SendAsync("SuspiciousEvent", new
        {
            sessionId = session.Id,
            request.LogType,
            request.Description,
            createdAt = log.CreatedAt,
            tabSwitchCount = session.TabSwitchCount
        }, cancellationToken);
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static List<OptionDto> ShuffleDeterministically(List<OptionDto> options, long sessionId, long questionId)
    {
        var seedInput = $"{sessionId}:{questionId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seedInput));
        var seed = BitConverter.ToInt32(hash, 0);

        var random = new Random(seed);
        return options.OrderBy(_ => random.Next()).ToList();
    }

    private static string GenerateAccessPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(8);
        var value = new char[8];

        for (var i = 0; i < value.Length; i++)
        {
            value[i] = chars[bytes[i] % chars.Length];
        }

        return new string(value);
    }

    private async Task<string> GenerateUniqueRefIdAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 20; i++)
        {
            var candidate = GenerateRefId();
            var exists = await _examRepository.RefIdExistsAsync(candidate, cancellationToken);
            if (!exists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Failed to generate unique Ref ID.");
    }

    private static string GenerateRefId()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(10);
        var value = new char[10];

        for (var i = 0; i < value.Length; i++)
        {
            value[i] = chars[bytes[i] % chars.Length];
        }

        return new string(value);
    }

    private async Task<int> GetQuestionOrderByQuestionId(long sessionId, long questionId, CancellationToken cancellationToken)
    {
        for (var i = 1; i <= 250; i++)
        {
            var q = await _examRepository.GetSessionQuestionAsync(sessionId, i, cancellationToken);
            if (q is null)
            {
                break;
            }

            if (q.QuestionId == questionId)
            {
                return i;
            }
        }

        return 1;
    }

    private async Task<int> CountCorrectAnswers(long sessionId, CancellationToken cancellationToken)
    {
        var count = 0;
        var total = await _examRepository.GetTotalQuestionCountAsync(sessionId, cancellationToken);
        for (var i = 1; i <= total; i++)
        {
            var examQuestion = await _examRepository.GetSessionQuestionAsync(sessionId, i, cancellationToken);
            if (examQuestion is null)
            {
                continue;
            }

            var answer = await _examRepository.GetAnswerAsync(sessionId, examQuestion.QuestionId, cancellationToken);
            if (answer?.IsCorrect == true)
            {
                count++;
            }
        }

        return count;
    }
}
