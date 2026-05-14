using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Table("tbl_t_practical_assessment_score")]
public class PracticalAssessmentScore
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("session_id")]
    public long SessionId { get; set; }

    [Column("template_item_id")]
    public long TemplateItemId { get; set; }

    [Column("numeric_value")]
    public decimal? NumericValue { get; set; }

    [MaxLength(20)]
    [Column("grade_value")]
    public string? GradeValue { get; set; }

    [MaxLength(500)]
    [Column("note")]
    public string? Note { get; set; }

    [ForeignKey(nameof(SessionId))]
    public PracticalAssessmentSession? Session { get; set; }

    [ForeignKey(nameof(TemplateItemId))]
    public PracticalAssessmentTemplateItem? TemplateItem { get; set; }
}
