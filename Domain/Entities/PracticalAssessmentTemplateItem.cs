using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Table("tbl_m_practical_template_item")]
public class PracticalAssessmentTemplateItem
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("template_id")]
    public long TemplateId { get; set; }

    [MaxLength(100)]
    [Column("section_name")]
    public string? SectionName { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("item_label")]
    public string ItemLabel { get; set; } = string.Empty;

    [Column("weight")]
    public decimal Weight { get; set; } = 1;

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [ForeignKey(nameof(TemplateId))]
    public PracticalAssessmentTemplate? Template { get; set; }

    public ICollection<PracticalAssessmentScore> Scores { get; set; } = new List<PracticalAssessmentScore>();
}
