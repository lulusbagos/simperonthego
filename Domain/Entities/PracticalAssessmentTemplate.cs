using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Table("tbl_m_practical_template")]
public class PracticalAssessmentTemplate
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("vehicle_id")]
    public long VehicleId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("template_name")]
    public string TemplateName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("scoring_mode")]
    public string ScoringMode { get; set; } = string.Empty;

    [Column("passing_score")]
    public decimal? PassingScore { get; set; }

    [MaxLength(20)]
    [Column("passing_grade")]
    public string? PassingGrade { get; set; }

    [MaxLength(200)]
    [Column("grade_options")]
    public string? GradeOptions { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CompanyId))]
    public Company? Company { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle? Vehicle { get; set; }

    public ICollection<PracticalAssessmentTemplateItem> Items { get; set; } = new List<PracticalAssessmentTemplateItem>();
    public ICollection<PracticalAssessmentSession> Sessions { get; set; } = new List<PracticalAssessmentSession>();
}
