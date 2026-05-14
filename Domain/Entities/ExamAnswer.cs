using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Table("tbl_t_exam_answer")]
public class ExamAnswer
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("session_id")]
    public long SessionId { get; set; }

    [Column("question_id")]
    public long QuestionId { get; set; }

    [MaxLength(1)]
    [Column("selected_answer")]
    public string? SelectedAnswer { get; set; }

    [Column("is_correct")]
    public bool IsCorrect { get; set; }

    [Column("answered_at")]
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SessionId))]
    public ExamSession? Session { get; set; }

    [ForeignKey(nameof(QuestionId))]
    public Question? Question { get; set; }
}
