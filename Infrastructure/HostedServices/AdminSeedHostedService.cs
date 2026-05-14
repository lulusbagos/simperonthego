using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimperSecureOnlineTestSystem.Domain.Entities;
using SimperSecureOnlineTestSystem.Domain.Enums;
using SimperSecureOnlineTestSystem.Infrastructure.Data;

namespace SimperSecureOnlineTestSystem.Infrastructure.HostedServices;

public sealed class AdminSeedHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AdminSeedHostedService> _logger;

    public AdminSeedHostedService(IServiceScopeFactory scopeFactory, ILogger<AdminSeedHostedService> logger)
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
            var passwordHasher = new PasswordHasher<UserLogin>();

            const string adminUsername = "admin";
            const string adminPassword = "admin";

            var existingAdmin = await dbContext.UserLogins
                .FirstOrDefaultAsync(x => x.Username == adminUsername, stoppingToken);

            if (existingAdmin is null)
            {
                var user = new UserLogin
                {
                    Username = adminUsername,
                    FullName = "System Administrator",
                    Role = SystemUserRole.Administrator,
                    CompanyId = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                user.PasswordHash = passwordHasher.HashPassword(user, adminPassword);
                dbContext.UserLogins.Add(user);
                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogDebug("Default admin user created with simplified credentials.");
                return;
            }

            existingAdmin.FullName = "System Administrator";
            existingAdmin.Role = SystemUserRole.Administrator;
            existingAdmin.CompanyId = null;
            existingAdmin.IsActive = true;
            existingAdmin.PasswordHash = passwordHasher.HashPassword(existingAdmin, adminPassword);
            await dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogDebug("Default admin credentials synchronized.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed default admin user during startup.");
        }
    }
}
