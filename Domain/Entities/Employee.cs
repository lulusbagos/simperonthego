using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Table("tbl_m_employee")]
public class Employee
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("nrp")]
    public string Nrp { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("employee_name")]
    public string EmployeeName { get; set; } = string.Empty;

    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CompanyId))]
    public Company? Company { get; set; }

    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
    public ICollection<PracticalAssessmentSession> PracticalAssessmentSessions { get; set; } = new List<PracticalAssessmentSession>();
}
