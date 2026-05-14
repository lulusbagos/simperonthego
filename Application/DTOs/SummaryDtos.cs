namespace SimperSecureOnlineTestSystem.Application.DTOs;

public class SummaryScoreDto
{
    public bool IsComplete { get; set; }
    public string SummaryId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Nrp { get; set; } = string.Empty;
    public string Ktp { get; set; } = string.Empty;
    public string SubmissionNumber { get; set; } = string.Empty;
    public string SubmissionType { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public DateTime TheoryScheduledAt { get; set; }
    public DateTime? TheoryFinishedAt { get; set; }
    public DateTime PracticalScheduledAt { get; set; }
    public DateTime? PracticalFinishedAt { get; set; }
    public decimal TheoryScore { get; set; }
    public bool TheoryPassed { get; set; }
    public int TheoryTotalQuestions { get; set; }
    public int TheoryCorrectAnswers { get; set; }
    public decimal? PracticalNumericScore { get; set; }
    public string? PracticalGrade { get; set; }
    public bool PracticalPassed { get; set; }
    public string PracticalTemplateName { get; set; } = string.Empty;
    public string PracticalScoringMode { get; set; } = string.Empty;
    public string? InstructorNote { get; set; }
    public long TheorySessionId { get; set; }
    public long PracticalSessionId { get; set; }
    public List<SummaryPracticalItemDto> PracticalItems { get; set; } = new();
}

public class SummaryPracticalItemDto
{
    public string SectionName { get; set; } = string.Empty;
    public string ItemLabel { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal? NumericValue { get; set; }
    public string? GradeValue { get; set; }
    public string? Note { get; set; }
}

