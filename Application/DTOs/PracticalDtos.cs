namespace SimperSecureOnlineTestSystem.Application.DTOs;

public class PracticalTemplateSummaryDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public long VehicleId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string ScoringMode { get; set; } = string.Empty;
    public decimal? PassingScore { get; set; }
    public string? PassingGrade { get; set; }
    public string? GradeOptions { get; set; }
    public bool IsActive { get; set; }
    public int SessionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PracticalTemplateItemDto> Items { get; set; } = new();
}

public class PracticalTemplateItemDto
{
    public long Id { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string ItemLabel { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public int DisplayOrder { get; set; }
}

public class PracticalTemplateUpsertDto
{
    public long CompanyId { get; set; }
    public long VehicleId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string ScoringMode { get; set; } = string.Empty;
    public decimal? PassingScore { get; set; }
    public string? PassingGrade { get; set; }
    public string? GradeOptions { get; set; }
    public List<PracticalTemplateItemInputDto> Items { get; set; } = new();
}

public class PracticalTemplateItemInputDto
{
    public string SectionName { get; set; } = string.Empty;
    public string ItemLabel { get; set; } = string.Empty;
    public decimal Weight { get; set; } = 1;
}

public class PracticalSessionSummaryDto
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Nrp { get; set; } = string.Empty;
    public string? Ktp { get; set; }
    public string? SubmissionNumber { get; set; }
    public string? SubmissionType { get; set; }
    public string? DepartmentName { get; set; }
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public long VehicleId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public long TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public long InstructorUserId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? FinalNumericScore { get; set; }
    public string? FinalGrade { get; set; }
    public bool? PassStatus { get; set; }
    public string? InstructorNote { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

public class PracticalSessionMoveDto
{
    public long SessionId { get; set; }
    public long VehicleId { get; set; }
    public DateTime ScheduledAt { get; set; }
}

public class PracticalSessionRemoveDto
{
    public long SessionId { get; set; }
}

public class PracticalSessionCreateDto
{
    public long EmployeeId { get; set; }
    public long VehicleId { get; set; }
    public long TemplateId { get; set; }
    public long InstructorUserId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public class PracticalEvaluationDto
{
    public long SessionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Nrp { get; set; } = string.Empty;
    public string? Ktp { get; set; }
    public string? SubmissionNumber { get; set; }
    public string? SubmissionType { get; set; }
    public string? DepartmentName { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string ScoringMode { get; set; } = string.Empty;
    public decimal? PassingScore { get; set; }
    public string? PassingGrade { get; set; }
    public string? GradeOptions { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public decimal? FinalNumericScore { get; set; }
    public string? FinalGrade { get; set; }
    public bool? PassStatus { get; set; }
    public string? InstructorNote { get; set; }
    public List<PracticalEvaluationItemDto> Items { get; set; } = new();
}

public class PracticalEvaluationItemDto
{
    public long TemplateItemId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string ItemLabel { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal? NumericValue { get; set; }
    public string? GradeValue { get; set; }
    public string? Note { get; set; }
}

public class PracticalEvaluationSubmitDto
{
    public long SessionId { get; set; }
    public string? InstructorNote { get; set; }
    public List<PracticalEvaluationItemSubmitDto> Items { get; set; } = new();
}

public class PracticalEvaluationItemSubmitDto
{
    public long TemplateItemId { get; set; }
    public decimal? NumericValue { get; set; }
    public string? GradeValue { get; set; }
    public string? Note { get; set; }
}
