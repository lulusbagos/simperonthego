using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SimperSecureOnlineTestSystem.Domain.Enums;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Table("tbl_t_exam_session")]
public class ExamSession
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("employee_id")]
    public long EmployeeId { get; set; }

    [Column("vehicle_id")]
    public long VehicleId { get; set; }

    [Required]
    [MaxLength(128)]
    [Column("token")]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    [Column("ref_id")]
    public string RefId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("access_password_hash")]
    public string AccessPasswordHash { get; set; } = string.Empty;

    [Column("start_time")]
    public DateTime? StartTime { get; set; }

    [Column("end_time")]
    public DateTime EndTime { get; set; }

    [Column("status")]
    public ExamSessionStatus Status { get; set; } = ExamSessionStatus.Pending;

    [Column("camera_active")]
    public bool CameraActive { get; set; }

    [Column("tab_switch_count")]
    public int TabSwitchCount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle? Vehicle { get; set; }

    public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
    public ICollection<ExamAnswer> ExamAnswers { get; set; } = new List<ExamAnswer>();
    public ICollection<ExamLog> ExamLogs { get; set; } = new List<ExamLog>();
    public ExamResult? ExamResult { get; set; }
}
