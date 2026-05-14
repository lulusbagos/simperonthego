using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Table("tbl_m_question")]
public class Question
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("vehicle_id")]
    public long VehicleId { get; set; }

    [Required]
    [Column("question_text")]
    public string QuestionText { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("option_a")]
    public string OptionA { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("option_b")]
    public string OptionB { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("option_c")]
    public string OptionC { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("option_d")]
    public string OptionD { get; set; } = string.Empty;

    [Required]
    [MaxLength(1)]
    [Column("correct_answer")]
    public string CorrectAnswer { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("difficulty")]
    public string Difficulty { get; set; } = "medium";

    [MaxLength(500)]
    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [MaxLength(500)]
    [Column("video_url")]
    public string? VideoUrl { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CompanyId))]
    public Company? Company { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle? Vehicle { get; set; }

    public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
    public ICollection<ExamAnswer> ExamAnswers { get; set; } = new List<ExamAnswer>();
}
