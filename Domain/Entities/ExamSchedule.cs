using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Table("tbl_t_exam_schedule")]
public class ExamSchedule
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("employee_id")]
    public long EmployeeId { get; set; }

    [Column("vehicle_id")]
    public long VehicleId { get; set; }

    [Column("scheduled_at")]
    public DateTime ScheduledAt { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "scheduled";

    [Column("created_by_user_id")]
    public long? CreatedByUserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle? Vehicle { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public UserLogin? CreatedByUser { get; set; }
}
