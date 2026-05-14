using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Table("tbl_r_exam_question")]
public class ExamQuestion
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("session_id")]
    public long SessionId { get; set; }

    [Column("question_id")]
    public long QuestionId { get; set; }

    [Column("question_order")]
    public int QuestionOrder { get; set; }

    [ForeignKey(nameof(SessionId))]
    public ExamSession? Session { get; set; }

    [ForeignKey(nameof(QuestionId))]
    public Question? Question { get; set; }
}
