using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Table("tbl_t_exam_result")]
public class ExamResult
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("session_id")]
    public long SessionId { get; set; }

    [Column("total_questions")]
    public int TotalQuestions { get; set; }

    [Column("correct_answers")]
    public int CorrectAnswers { get; set; }

    [Column("score")]
    public decimal Score { get; set; }

    [Column("pass_status")]
    public bool PassStatus { get; set; }

    [Column("finished_at")]
    public DateTime FinishedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SessionId))]
    public ExamSession? Session { get; set; }
}
