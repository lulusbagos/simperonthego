using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SimperSecureOnlineTestSystem.Application.DTOs;
using SimperSecureOnlineTestSystem.Domain.Entities;

namespace SimperSecureOnlineTestSystem.ViewModels;

public class NrpInputViewModel
{
    [Required]
    [Display(Name = "NRP")]
    [MaxLength(50)]
    public string Nrp { get; set; } = string.Empty;
}

public class RefIdInputViewModel
{
    [Required]
    [Display(Name = "Ref ID")]
    [MaxLength(32)]
    [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "Ref ID hanya boleh huruf dan angka.")]
    public string RefId { get; set; } = string.Empty;
}

public class SelectExamViewModel
{
    [Required]
    public string Nrp { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Vehicle")]
    public long VehicleId { get; set; }

    public List<Vehicle> Vehicles { get; set; } = new();
}

public class ExamStartViewModel
{
    public string Token { get; set; } = string.Empty;
    public DateTime EndTime { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SessionGeneratedViewModel
{
    public string Token { get; set; } = string.Empty;
    public string RefId { get; set; } = string.Empty;
    public string AccessPassword { get; set; } = string.Empty;
    public string ExamLink { get; set; } = string.Empty;
    public DateTime EndTime { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
}

public class ExamAccessViewModel
{
    public string Token { get; set; } = string.Empty;
    public string RefId { get; set; } = string.Empty;
}

public class ExamResultViewModel
{
    public string Token { get; set; } = string.Empty;
    public ExamResultDto Result { get; set; } = new();
}

public class AdminDashboardViewModel
{
    public List<AdminExamMonitorDto> ActiveExams { get; set; } = new();
    public List<DashboardCompanySummaryViewModel> CompanySummaries { get; set; } = new();
    public bool IsAdministrator { get; set; }
}

public class DashboardCompanySummaryViewModel
{
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int ActiveExamCount { get; set; }
    public int CameraOffCount { get; set; }
    public int SuspiciousCount { get; set; }
    public decimal AverageScore { get; set; }
}

public class CompanyManageViewModel
{
    public long CompanyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    public List<Company> ExistingCompanies { get; set; } = new();
}

public class ProfileManageViewModel
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public long? CompanyId { get; set; }
    public string? ProfilePhotoUrl { get; set; }

    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    public IFormFile? ProfilePhoto { get; set; }
}

public class EmployeeManageViewModel
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalApplicants { get; set; }

    [Required]
    [MaxLength(50)]
    public string Nrp { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string EmployeeName { get; set; } = string.Empty;

    [Required]
    public long CompanyId { get; set; }

    public string? SearchTerm { get; set; }
    public bool IsAdministrator { get; set; }
    public List<Company> ExistingCompanies { get; set; } = new();
    public List<Employee> ExistingEmployees { get; set; } = new();
    public List<SimperApplicantView> EligibleApplicants { get; set; } = new();

    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling((double)TotalApplicants / PageSize));
}

public class VehicleManageViewModel
{
    [Required]
    public long CompanyId { get; set; }

    [Required]
    [MaxLength(150)]
    public string VehicleName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string SimperType { get; set; } = string.Empty;

    public bool IsAdministrator { get; set; }
    public List<Company> ExistingCompanies { get; set; } = new();
    public List<Vehicle> ExistingVehicles { get; set; } = new();
}

public class QuestionManageViewModel
{
    [Required]
    public long CompanyId { get; set; }

    [Required]
    public long VehicleId { get; set; }

    [Required]
    public string QuestionText { get; set; } = string.Empty;

    [Required]
    public string OptionA { get; set; } = string.Empty;

    [Required]
    public string OptionB { get; set; } = string.Empty;

    [Required]
    public string OptionC { get; set; } = string.Empty;

    [Required]
    public string OptionD { get; set; } = string.Empty;

    [Required]
    [RegularExpression("A|B|C|D")]
    public string CorrectAnswer { get; set; } = "A";

    public string Difficulty { get; set; } = "medium";

    public IFormFile? ImageFile { get; set; }
    public IFormFile? VideoFile { get; set; }
    public IFormFile? BulkExcelFile { get; set; }
    public long? ImportCompanyId { get; set; }
    public long? ImportVehicleId { get; set; }

    public string? ImportSummaryMessage { get; set; }
    public int ImportTotalCount { get; set; }
    public int ImportSuccessCount { get; set; }
    public int ImportFailedCount { get; set; }

    public List<Company> ExistingCompanies { get; set; } = new();
    public List<Vehicle> ExistingVehicles { get; set; } = new();
    public List<Question> ExistingQuestions { get; set; } = new();
    public List<QuestionImportRowViewModel> ImportRows { get; set; } = new();
}

public class QuestionImportRowViewModel
{
    public int RowNumber { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}
