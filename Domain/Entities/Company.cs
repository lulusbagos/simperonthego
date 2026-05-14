using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Table("tbl_m_company")]
public class Company
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("company_name")]
    public string CompanyName { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<PracticalAssessmentTemplate> PracticalAssessmentTemplates { get; set; } = new List<PracticalAssessmentTemplate>();
}
