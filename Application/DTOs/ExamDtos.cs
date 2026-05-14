namespace SimperSecureOnlineTestSystem.Application.DTOs;

public record SessionCreateRequestDto(string Nrp, long VehicleId, int DurationMinutes = 60);
public record SessionCreateResponseDto(long SessionId, string Token, string RefId, string AccessPassword, DateTime EndTime, string EmployeeName, string VehicleName);

public record OptionDto(string Key, string Text);

public class ExamQuestionDto
{
    public long SessionId { get; set; }
    public int Order { get; set; }
    public long QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public List<OptionDto> Options { get; set; } = new();
    public string? SelectedAnswer { get; set; }
    public int TotalQuestions { get; set; }
    public DateTime EndTime { get; set; }
}

public record SaveAnswerRequestDto(string Token, long QuestionId, string SelectedAnswer);
public record SaveAnswerResponseDto(bool Saved, int AnsweredCount, int TotalQuestions);

public record LogEventDto(string Token, string LogType, string Description);
public record CameraStatusDto(string Token, bool IsActive);

public class AdminExamMonitorDto
{
    public long SessionId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string RefId { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public long VehicleId { get; set; }
    public string Nrp { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool CameraActive { get; set; }
    public int TabSwitchCount { get; set; }
    public int AnsweredCount { get; set; }
    public int TotalQuestions { get; set; }
    public decimal Score { get; set; }
}

public record SessionValidationDto(bool IsValid, string Message, long SessionId, DateTime EndTime, bool Started);

public class ExamResultDto
{
    public long SessionId { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public decimal Score { get; set; }
    public bool PassStatus { get; set; }
    public DateTime FinishedAt { get; set; }
}

public class EmployeeResultDto
{
    public long SessionId { get; set; }
    public long VehicleId { get; set; }
    public string Nrp { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public bool PassStatus { get; set; }
    public DateTime FinishedAt { get; set; }
}

public class AnswerReviewDto
{
    public long SessionId { get; set; }
    public string Nrp { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public List<AnswerReviewItemDto> Items { get; set; } = new();
}

public class AnswerReviewItemDto
{
    public int Order { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? SelectedAnswer { get; set; }
    public bool IsCorrect { get; set; }
}
