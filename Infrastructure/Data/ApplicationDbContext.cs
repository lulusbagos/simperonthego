using Microsoft.EntityFrameworkCore;
using SimperSecureOnlineTestSystem.Domain.Entities;

namespace SimperSecureOnlineTestSystem.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyDirectoryView> CompanyDirectories => Set<CompanyDirectoryView>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<UserLogin> UserLogins => Set<UserLogin>();
    public DbSet<ExamSession> ExamSessions => Set<ExamSession>();
    public DbSet<ExamSchedule> ExamSchedules => Set<ExamSchedule>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<ExamAnswer> ExamAnswers => Set<ExamAnswer>();
    public DbSet<ExamResult> ExamResults => Set<ExamResult>();
    public DbSet<ExamLog> ExamLogs => Set<ExamLog>();
    public DbSet<SimperApplicantView> SimperApplicants => Set<SimperApplicantView>();
    public DbSet<PermitApprovalView> PermitApprovals => Set<PermitApprovalView>();
    public DbSet<PracticalAssessmentTemplate> PracticalAssessmentTemplates => Set<PracticalAssessmentTemplate>();
    public DbSet<PracticalAssessmentTemplateItem> PracticalAssessmentTemplateItems => Set<PracticalAssessmentTemplateItem>();
    public DbSet<PracticalAssessmentSession> PracticalAssessmentSessions => Set<PracticalAssessmentSession>();
    public DbSet<PracticalAssessmentScore> PracticalAssessmentScores => Set<PracticalAssessmentScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>()
            .HasIndex(x => x.Nrp)
            .IsUnique();

        modelBuilder.Entity<CompanyDirectoryView>()
            .HasNoKey()
            .ToView("vw_company", "public");

        modelBuilder.Entity<SimperApplicantView>()
            .HasNoKey()
            .ToView("vw_pengajuan_simper", "public");

        modelBuilder.Entity<PermitApprovalView>()
            .HasNoKey()
            .ToView("vw_permit_approval", "public");

        modelBuilder.Entity<ExamSession>()
            .HasIndex(x => x.Token)
            .IsUnique();

        modelBuilder.Entity<ExamSession>()
            .HasIndex(x => x.RefId)
            .IsUnique();

        modelBuilder.Entity<ExamSchedule>()
            .HasIndex(x => new { x.VehicleId, x.ScheduledAt })
            .IsUnique(false);

        modelBuilder.Entity<ExamSchedule>()
            .HasIndex(x => new { x.EmployeeId, x.VehicleId, x.ScheduledAt })
            .IsUnique();

        modelBuilder.Entity<UserLogin>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<ExamQuestion>()
            .HasIndex(x => new { x.SessionId, x.QuestionOrder })
            .IsUnique();

        modelBuilder.Entity<ExamQuestion>()
            .HasIndex(x => new { x.SessionId, x.QuestionId })
            .IsUnique();

        modelBuilder.Entity<ExamAnswer>()
            .HasIndex(x => new { x.SessionId, x.QuestionId })
            .IsUnique();

        modelBuilder.Entity<ExamResult>()
            .HasIndex(x => x.SessionId)
            .IsUnique();

        modelBuilder.Entity<PracticalAssessmentTemplate>()
            .HasIndex(x => new { x.CompanyId, x.VehicleId, x.IsActive });

        modelBuilder.Entity<PracticalAssessmentTemplateItem>()
            .HasIndex(x => new { x.TemplateId, x.DisplayOrder })
            .IsUnique();

        modelBuilder.Entity<PracticalAssessmentSession>()
            .HasIndex(x => new { x.EmployeeId, x.VehicleId, x.ScheduledAt });

        modelBuilder.Entity<PracticalAssessmentSession>()
            .HasIndex(x => new { x.InstructorUserId, x.ScheduledAt });

        modelBuilder.Entity<PracticalAssessmentScore>()
            .HasIndex(x => new { x.SessionId, x.TemplateItemId })
            .IsUnique();
    }
}
