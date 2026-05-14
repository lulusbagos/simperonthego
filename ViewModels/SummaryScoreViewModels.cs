using SimperSecureOnlineTestSystem.Application.DTOs;
using SimperSecureOnlineTestSystem.Domain.Enums;

namespace SimperSecureOnlineTestSystem.ViewModels;

public class SummaryScoreViewModel
{
    public bool CanPrint => IsComplete;
    public bool IsComplete { get; set; }
    public string SummaryId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = "PT INDEXIM COALINDO";
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
    public string QrProofText { get; set; } = string.Empty;
    public string QrTargetUrl { get; set; } = string.Empty;
    public string QrCodeDataUrl { get; set; } = string.Empty;
    public string AppLogoPath { get; set; } = string.Empty;
    public string CompanyLogoPath { get; set; } = string.Empty;
    public List<SummaryPracticalItemDto> PracticalItems { get; set; } = new();

    public string PracticalDisplayScore =>
        string.Equals(PracticalScoringMode, Domain.Enums.PracticalScoringMode.Numeric, StringComparison.OrdinalIgnoreCase)
            ? (PracticalNumericScore?.ToString("0.##") ?? "-")
            : (PracticalGrade ?? "-");

    public string FinalStatus => TheoryPassed && PracticalPassed ? "LULUS" : "TIDAK LULUS";
}
