using Microsoft.EntityFrameworkCore;
using SimperSecureOnlineTestSystem.Infrastructure.Data;

namespace SimperSecureOnlineTestSystem.Infrastructure.HostedServices;

public sealed class ScheduleCapacitySchemaHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduleCapacitySchemaHostedService> _logger;

    public ScheduleCapacitySchemaHostedService(IServiceScopeFactory scopeFactory, ILogger<ScheduleCapacitySchemaHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!dbContext.Database.IsNpgsql())
            {
                return;
            }

            await dbContext.Database.ExecuteSqlRawAsync("""
                DROP INDEX IF EXISTS "IX_ExamSchedules_VehicleId_ScheduledAt";
                DROP INDEX IF EXISTS "IX_tbl_t_exam_schedule_vehicle_id_scheduled_at";
                DROP INDEX IF EXISTS "ix_tbl_t_exam_schedule_vehicle_id_scheduled_at";
                CREATE INDEX IF NOT EXISTS "IX_tbl_t_exam_schedule_vehicle_id_scheduled_at"
                ON tbl_t_exam_schedule (vehicle_id, scheduled_at);
                """, stoppingToken);

            _logger.LogDebug("Schedule slot index normalized for multi-participant sessions.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to normalize schedule slot index.");
        }
    }
}
