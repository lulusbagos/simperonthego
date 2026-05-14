using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Table("tbl_t_practical_assessment")]
public class PracticalAssessmentSession
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("employee_id")]
    public long EmployeeId { get; set; }

    [Column("vehicle_id")]
    public long VehicleId { get; set; }

    [Column("template_id")]
    public long TemplateId { get; set; }

    [Column("instructor_user_id")]
    public long InstructorUserId { get; set; }

    [Column("scheduled_at")]
    public DateTime ScheduledAt { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("final_numeric_score")]
    public decimal? FinalNumericScore { get; set; }

    [MaxLength(20)]
    [Column("final_grade")]
    public string? FinalGrade { get; set; }

    [Column("pass_status")]
    public bool? PassStatus { get; set; }

    [MaxLength(1000)]
    [Column("instructor_note")]
    public string? InstructorNote { get; set; }

    [Column("created_by_user_id")]
    public long? CreatedByUserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle? Vehicle { get; set; }

    [ForeignKey(nameof(TemplateId))]
    public PracticalAssessmentTemplate? Template { get; set; }

    [ForeignKey(nameof(InstructorUserId))]
    [InverseProperty(nameof(UserLogin.PracticalAssignments))]
    public UserLogin? InstructorUser { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    [InverseProperty(nameof(UserLogin.CreatedPracticalSessions))]
    public UserLogin? CreatedByUser { get; set; }

    public ICollection<PracticalAssessmentScore> Scores { get; set; } = new List<PracticalAssessmentScore>();
}
