using System.ComponentModel.DataAnnotations;
using SimperSecureOnlineTestSystem.Application.DTOs;
using SimperSecureOnlineTestSystem.Domain.Entities;

namespace SimperSecureOnlineTestSystem.ViewModels;

public class PracticalTemplateManageViewModel
{
    public long? TemplateId { get; set; }

    [Required]
    public long CompanyId { get; set; }

    [Required]
    public long VehicleId { get; set; }

    [Required]
    [MaxLength(150)]
    public string TemplateName { get; set; } = string.Empty;

    [Required]
    public string ScoringMode { get; set; } = string.Empty;

    public decimal? PassingScore { get; set; }

    public string? PassingGrade { get; set; }

    public string GradeOptions { get; set; } = "A,B,C,D";

    [Required]
    public string ItemDefinitions { get; set; } = string.Empty;

    public string? SearchTerm { get; set; }

    public bool IsAdministrator { get; set; }
    public List<Company> ExistingCompanies { get; set; } = new();
    public List<Vehicle> ExistingVehicles { get; set; } = new();
    public List<PracticalTemplateSummaryDto> ExistingTemplates { get; set; } = new();
}

public class PracticalSessionManageViewModel
{
    [Required]
    public long EmployeeId { get; set; }

    [Required]
    public long VehicleId { get; set; }

    [Required]
    public long TemplateId { get; set; }

    [Required]
    public long InstructorUserId { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; } = DateTime.Today.AddHours(8);

    public DateTime Date { get; set; } = DateTime.Today;

    public bool IsAdministrator { get; set; }
    public List<Company> ExistingCompanies { get; set; } = new();
    public List<Employee> ExistingEmployees { get; set; } = new();
    public List<Vehicle> ExistingVehicles { get; set; } = new();
    public List<UserLogin> ExistingInstructors { get; set; } = new();
    public List<PracticalTemplateSummaryDto> ExistingTemplates { get; set; } = new();
    public List<PracticalSessionSummaryDto> ExistingSessions { get; set; } = new();
    public List<string> TheoryCompletedKeys { get; set; } = new();
}

public class PracticalMyAssignmentsViewModel
{
    public string UserRole { get; set; } = string.Empty;
    public List<PracticalSessionSummaryDto> Sessions { get; set; } = new();
}

public class InstructorDirectoryViewModel
{
    public bool IsAdministrator { get; set; }
    public string? SearchTerm { get; set; }
    public List<UserLogin> Instructors { get; set; } = new();
    public List<PracticalSessionSummaryDto> Sessions { get; set; } = new();
}

public class PracticalResultsViewModel
{
    public bool IsAdministrator { get; set; }
    public string? SearchTerm { get; set; }
    public List<PracticalSessionSummaryDto> Sessions { get; set; } = new();
}

public class PracticalEvaluationInputViewModel
{
    public long SessionId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;
    public string Nrp { get; set; } = string.Empty;
    public string? Ktp { get; set; }
    public string? SubmissionNumber { get; set; }
    public string? SubmissionType { get; set; }
    public string? DepartmentName { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string ScoringMode { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public decimal? PassingScore { get; set; }
    public string? PassingGrade { get; set; }
    public string GradeOptions { get; set; } = string.Empty;
    public decimal? FinalNumericScore { get; set; }
    public string? FinalGrade { get; set; }
    public bool? PassStatus { get; set; }
    public string? ExistingInstructorNote { get; set; }

    [MaxLength(1000)]
    public string? InstructorNote { get; set; }

    public List<PracticalEvaluationItemInputViewModel> Items { get; set; } = new();
}

public class PracticalEvaluationItemInputViewModel
{
    public long TemplateItemId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string ItemLabel { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal? NumericValue { get; set; }
    public string? GradeValue { get; set; }
    public string? Note { get; set; }
}
