using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Table("tbl_t_exam_log")]
public class ExamLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("session_id")]
    public long SessionId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("log_type")]
    public string LogType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SessionId))]
    public ExamSession? Session { get; set; }
}
