using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Table("tbl_m_vehicle")]
public class Vehicle
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("vehicle_name")]
    public string VehicleName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("simper_type")]
    public string SimperType { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CompanyId))]
    public Company? Company { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
    public ICollection<PracticalAssessmentTemplate> PracticalAssessmentTemplates { get; set; } = new List<PracticalAssessmentTemplate>();
    public ICollection<PracticalAssessmentSession> PracticalAssessmentSessions { get; set; } = new List<PracticalAssessmentSession>();
}
