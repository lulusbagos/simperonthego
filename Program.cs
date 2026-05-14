using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using SimperSecureOnlineTestSystem.Domain.Entities;
using SimperSecureOnlineTestSystem.Domain.Enums;
using SimperSecureOnlineTestSystem.Application.Services;
using SimperSecureOnlineTestSystem.Hubs;
using SimperSecureOnlineTestSystem.Infrastructure.Data;
using SimperSecureOnlineTestSystem.Infrastructure.HostedServices;
using SimperSecureOnlineTestSystem.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.
builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
    options.UseNpgsql(defaultConnection, npgsql =>
    {
        npgsql.CommandTimeout(15);
        npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
    }));

builder.Services.AddSignalR();
builder.Services.AddControllersWithViews();
builder.Services.AddHostedService<AdminSeedHostedService>();
builder.Services.AddHostedService<ScheduleCapacitySchemaHostedService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminAccess", policy =>
    {
        policy.RequireRole(SystemUserRole.Administrator, SystemUserRole.CompanyAdmin);
    });
});

builder.Services.AddScoped<IExamRepository, ExamRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IAdminService, AdminService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "admin-login",
    pattern: "Admin/Login",
    defaults: new { controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Exam}/{action=EnterRefId}/{id?}");

app.MapHub<MonitoringHub>("/hubs/monitoring");

if (args.Any(x => string.Equals(x, "--reset-masterdata-seed", StringComparison.OrdinalIgnoreCase)))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await ResetMasterDataAndSeedDemoAsync(dbContext);
    Console.WriteLine("Seed selesai: company, vehicle, dan question demo berhasil dibuat.");
    return;
}

if (args.Any(x => string.Equals(x, "--seed-complete-dummy-scenario", StringComparison.OrdinalIgnoreCase)))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SeedCompleteDummyScenarioAsync(dbContext);
    return;
}

if (args.Any(x => string.Equals(x, "--seed-dummy-outcome-matrix", StringComparison.OrdinalIgnoreCase)))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SeedDummyOutcomeMatrixAsync(dbContext);
    return;
}

var appUrl = builder.Configuration["App:Url"];
if (string.IsNullOrWhiteSpace(appUrl))
{
    appUrl = "http://localhost:5088";
}

app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine($"[Simper] Running on: {appUrl}");
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("[Simper] Application is stopping...");
});

app.Run(appUrl);

static async Task ResetMasterDataAndSeedDemoAsync(ApplicationDbContext dbContext)
{
    var strategy = dbContext.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(async () =>
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync();

        await dbContext.PracticalAssessmentScores.ExecuteDeleteAsync();
        await dbContext.PracticalAssessmentSessions.ExecuteDeleteAsync();
        await dbContext.PracticalAssessmentTemplateItems.ExecuteDeleteAsync();
        await dbContext.PracticalAssessmentTemplates.ExecuteDeleteAsync();
        await dbContext.ExamLogs.ExecuteDeleteAsync();
        await dbContext.ExamAnswers.ExecuteDeleteAsync();
        await dbContext.ExamQuestions.ExecuteDeleteAsync();
        await dbContext.ExamResults.ExecuteDeleteAsync();
        await dbContext.ExamSchedules.ExecuteDeleteAsync();
        await dbContext.ExamSessions.ExecuteDeleteAsync();
        await dbContext.Questions.ExecuteDeleteAsync();
        await dbContext.Vehicles.ExecuteDeleteAsync();
        await dbContext.Employees.ExecuteDeleteAsync();

    var now = DateTime.UtcNow;
    var preferredCompanyNames = new[]
    {
        "PT INDEXIM COALINDO",
        "PT KALIMANTAN PRIMA PERSADA",
        "PT UNGGUL DINAMIKA UTAMA",
        "PT MEGA GLOBAL ENERGI"
    };

    var activeScopedUserCompanyIds = await dbContext.UserLogins
        .AsNoTracking()
        .Where(x => x.IsActive && x.CompanyId != null)
        .Select(x => x.CompanyId!.Value)
        .Distinct()
        .ToListAsync();

    var allCompanyViews = await dbContext.CompanyDirectories
        .AsNoTracking()
        .Where(x => !string.IsNullOrWhiteSpace(x.CompanyName))
        .OrderBy(x => x.CompanyName)
        .ToListAsync();

    var selectedCompanyViews = allCompanyViews
        .Where(x => preferredCompanyNames.Contains((x.CompanyName ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase) ||
                    activeScopedUserCompanyIds.Contains(x.Id))
        .ToList();

    if (selectedCompanyViews.Count == 0)
    {
        selectedCompanyViews = allCompanyViews.Take(4).ToList();
    }

    if (selectedCompanyViews.Count == 0)
    {
        throw new InvalidOperationException("vw_company tidak memiliki data company untuk di-seed.");
    }

        var existingCompanies = await dbContext.Companies.ToListAsync();
        var companies = new List<Company>();
        foreach (var view in selectedCompanyViews)
        {
            var existingCompany = existingCompanies.FirstOrDefault(x => x.Id == view.Id);
            if (existingCompany is null)
            {
                existingCompany = new Company
                {
                    Id = view.Id,
                    CompanyName = (view.CompanyName ?? $"Company {view.Id}").Trim(),
                    CreatedAt = now
                };
                await dbContext.Companies.AddAsync(existingCompany);
            }
            else
            {
                existingCompany.CompanyName = (view.CompanyName ?? $"Company {view.Id}").Trim();
            }

            companies.Add(existingCompany);
        }

        await dbContext.SaveChangesAsync();
        await ResetIdentitySequenceAsync(dbContext, "public.tbl_m_company", "id");

    var vehicleDefinitions = new[]
    {
        ("HD 785", "Dump Truck"),
        ("LV", "Light Vehicle"),
        ("Crane Truck", "Crane"),
        ("PC 200", "Excavator"),
        ("PC 1000", "Excavator"),
        ("PC 2000", "Excavator")
    };

    var vehicles = new List<Vehicle>();
    foreach (var company in companies)
    {
        foreach (var (vehicleName, simperType) in vehicleDefinitions)
        {
            vehicles.Add(new Vehicle
            {
                CompanyId = company.Id,
                VehicleName = vehicleName,
                SimperType = simperType,
                CreatedAt = now
            });
        }
    }

        await dbContext.Vehicles.AddRangeAsync(vehicles);
        await dbContext.SaveChangesAsync();

    var questions = new List<Question>();
    foreach (var company in companies)
    {
        var companyVehicles = vehicles.Where(v => v.CompanyId == company.Id).ToList();
        foreach (var vehicle in companyVehicles)
        {
            var templates = GetQuestionTemplatesByVehicle(vehicle.VehicleName);
            foreach (var template in templates)
            {
                questions.Add(new Question
                {
                    CompanyId = company.Id,
                    VehicleId = vehicle.Id,
                    QuestionText = template.QuestionText,
                    OptionA = template.OptionA,
                    OptionB = template.OptionB,
                    OptionC = template.OptionC,
                    OptionD = template.OptionD,
                    CorrectAnswer = template.CorrectAnswer,
                    Difficulty = template.Difficulty,
                    CreatedAt = now
                });
            }
        }
    }

        await dbContext.Questions.AddRangeAsync(questions);
        await dbContext.SaveChangesAsync();

    var selectedCompanyIds = companies.Select(x => x.Id).ToHashSet();
    var applicants = await dbContext.SimperApplicants
        .AsNoTracking()
        .Where(x => x.CreatedCompanyId.HasValue &&
                    selectedCompanyIds.Contains(x.CreatedCompanyId.Value) &&
                    !string.IsNullOrWhiteSpace(x.Nik))
        .OrderByDescending(x => x.Id)
        .ToListAsync();

    var employees = applicants
        .GroupBy(x => x.Nik!.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase)
        .Select(group => group.First())
        .Select(applicant => new Employee
        {
            Nrp = applicant.Nik!.Trim(),
            EmployeeName = string.IsNullOrWhiteSpace(applicant.Nama) ? applicant.Nik!.Trim() : applicant.Nama.Trim(),
            CompanyId = applicant.CreatedCompanyId!.Value,
            CreatedAt = now
        })
        .ToList();

        if (employees.Count > 0)
        {
            await dbContext.Employees.AddRangeAsync(employees);
            await dbContext.SaveChangesAsync();
            await ResetIdentitySequenceAsync(dbContext, "public.tbl_m_employee", "id");
        }

    var practicalTemplates = new List<PracticalAssessmentTemplate>();
    foreach (var vehicle in vehicles)
    {
        practicalTemplates.Add(new PracticalAssessmentTemplate
        {
            CompanyId = vehicle.CompanyId,
            VehicleId = vehicle.Id,
            TemplateName = $"Form Praktek {vehicle.VehicleName} - Dummy",
            ScoringMode = vehicle.VehicleName.Equals("LV", StringComparison.OrdinalIgnoreCase)
                ? PracticalScoringMode.Grade
                : PracticalScoringMode.Numeric,
            PassingScore = vehicle.VehicleName.Equals("LV", StringComparison.OrdinalIgnoreCase) ? null : 75,
            PassingGrade = vehicle.VehicleName.Equals("LV", StringComparison.OrdinalIgnoreCase) ? "B" : null,
            GradeOptions = vehicle.VehicleName.Equals("LV", StringComparison.OrdinalIgnoreCase) ? "A,B,C,D" : null,
            IsActive = true,
            CreatedAt = now
        });
    }

        await dbContext.PracticalAssessmentTemplates.AddRangeAsync(practicalTemplates);
        await dbContext.SaveChangesAsync();

    var practicalItems = new List<PracticalAssessmentTemplateItem>();
    foreach (var template in practicalTemplates)
    {
        foreach (var (sectionName, itemLabel, weight, order) in GetPracticalTemplateDefinitions(template.TemplateName).Select((item, index) => (item.SectionName, item.ItemLabel, item.Weight, index + 1)))
        {
            practicalItems.Add(new PracticalAssessmentTemplateItem
            {
                TemplateId = template.Id,
                SectionName = sectionName,
                ItemLabel = itemLabel,
                Weight = weight,
                DisplayOrder = order
            });
        }
    }

        await dbContext.PracticalAssessmentTemplateItems.AddRangeAsync(practicalItems);

        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        Console.WriteLine($"Company dibuat: {companies.Count}");
        Console.WriteLine($"Employee dari view dibuat: {employees.Count}");
        Console.WriteLine($"Vehicle dibuat: {vehicles.Count}");
        Console.WriteLine($"Question dibuat: {questions.Count}");
        Console.WriteLine($"Template praktek dibuat: {practicalTemplates.Count}");
        Console.WriteLine($"Item template praktek dibuat: {practicalItems.Count}");
    });
}

static async Task ResetIdentitySequenceAsync(ApplicationDbContext dbContext, string tableName, string columnName)
{
    await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
        SELECT setval(
            pg_get_serial_sequence('{tableName}', '{columnName}'),
            COALESCE((SELECT MAX({columnName}) FROM {tableName}), 1),
            true
        );");
}

static async Task SeedCompleteDummyScenarioAsync(ApplicationDbContext dbContext)
{
    var strategy = dbContext.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(async () =>
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;
        var template = await dbContext.PracticalAssessmentTemplates
            .Include(x => x.Items)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.IsActive && x.ScoringMode == PracticalScoringMode.Numeric);

        if (template is null)
        {
            throw new InvalidOperationException("Template praktek numeric aktif belum tersedia. Jalankan --reset-masterdata-seed lebih dulu.");
        }

        var employee = await dbContext.Employees
            .Include(x => x.Company)
            .Where(x => x.CompanyId == template.CompanyId)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

        if (employee is null)
        {
            throw new InvalidOperationException("Belum ada employee internal untuk company template dummy.");
        }

        var questions = await dbContext.Questions
            .Where(x => x.CompanyId == template.CompanyId && x.VehicleId == template.VehicleId)
            .OrderBy(x => x.Id)
            .Take(5)
            .ToListAsync();

        if (questions.Count < 5)
        {
            throw new InvalidOperationException("Soal teori minimal 5 belum tersedia untuk unit dummy.");
        }

        var userHasher = new PasswordHasher<UserLogin>();
        var examHasher = new PasswordHasher<ExamSession>();

        var companyAdmin = await EnsureDemoUserAsync(
            dbContext,
            userHasher,
            "company.demo.complete",
            "Company Demo Complete",
            SystemUserRole.CompanyAdmin,
            template.CompanyId,
            "Demo@123");

        var instructor = await EnsureDemoUserAsync(
            dbContext,
            userHasher,
            "instructor.demo.complete",
            "Instructor Demo Complete",
            SystemUserRole.Instructor,
            template.CompanyId,
            "Demo@123");

        var theoryScheduleAt = now.Date.AddHours(9);
        var practicalScheduleAt = now.Date.AddHours(13);

        var examSchedule = new ExamSchedule
        {
            EmployeeId = employee.Id,
            VehicleId = template.VehicleId,
            ScheduledAt = theoryScheduleAt,
            Status = "completed",
            CreatedByUserId = companyAdmin.Id,
            CreatedAt = now.AddMinutes(-80)
        };
        await dbContext.ExamSchedules.AddAsync(examSchedule);

        var examSession = new ExamSession
        {
            EmployeeId = employee.Id,
            VehicleId = template.VehicleId,
            Token = $"dummy-theory-{Guid.NewGuid():N}",
            RefId = $"DUM{DateTime.UtcNow:MMddHHmmss}",
            AccessPasswordHash = string.Empty,
            StartTime = theoryScheduleAt,
            EndTime = theoryScheduleAt.AddMinutes(50),
            Status = ExamSessionStatus.Completed,
            CameraActive = true,
            TabSwitchCount = 0,
            CreatedAt = now.AddMinutes(-90)
        };
        examSession.AccessPasswordHash = examHasher.HashPassword(examSession, "Demo123");

        await dbContext.ExamSessions.AddAsync(examSession);
        await dbContext.SaveChangesAsync();

        var selectedQuestions = questions
            .Select((question, index) => new ExamQuestion
            {
                SessionId = examSession.Id,
                QuestionId = question.Id,
                QuestionOrder = index + 1
            })
            .ToList();
        await dbContext.ExamQuestions.AddRangeAsync(selectedQuestions);

        var examAnswers = questions
            .Select((question, index) => new ExamAnswer
            {
                SessionId = examSession.Id,
                QuestionId = question.Id,
                SelectedAnswer = index == questions.Count - 1
                    ? (new[] { "A", "B", "C", "D" }.First(option => option != question.CorrectAnswer))
                    : question.CorrectAnswer,
                IsCorrect = index != questions.Count - 1,
                AnsweredAt = theoryScheduleAt.AddMinutes(10 + (index * 3))
            })
            .ToList();
        await dbContext.ExamAnswers.AddRangeAsync(examAnswers);

        var examResult = new ExamResult
        {
            SessionId = examSession.Id,
            TotalQuestions = questions.Count,
            CorrectAnswers = questions.Count - 1,
            Score = 80,
            PassStatus = true,
            FinishedAt = theoryScheduleAt.AddMinutes(48)
        };
        await dbContext.ExamResults.AddAsync(examResult);

        var practicalSession = new PracticalAssessmentSession
        {
            EmployeeId = employee.Id,
            VehicleId = template.VehicleId,
            TemplateId = template.Id,
            InstructorUserId = instructor.Id,
            ScheduledAt = practicalScheduleAt,
            Status = PracticalAssessmentStatus.Submitted,
            FinalNumericScore = 86,
            FinalGrade = null,
            PassStatus = true,
            InstructorNote = "Dummy lulus. Kontrol unit stabil, awareness terhadap safety baik, dan shutdown sesuai SOP.",
            CreatedByUserId = companyAdmin.Id,
            CreatedAt = now.AddMinutes(-40),
            SubmittedAt = practicalScheduleAt.AddMinutes(35)
        };
        await dbContext.PracticalAssessmentSessions.AddAsync(practicalSession);
        await dbContext.SaveChangesAsync();

        var practicalScores = template.Items
            .OrderBy(x => x.DisplayOrder)
            .Select((item, index) => new PracticalAssessmentScore
            {
                SessionId = practicalSession.Id,
                TemplateItemId = item.Id,
                NumericValue = 82 + (index % 3 * 4),
                Note = index == 0
                    ? "Pemeriksaan awal lengkap."
                    : index == template.Items.Count - 1
                        ? "Proses shutdown aman dan rapi."
                        : "Kontrol operasi baik."
            })
            .ToList();
        await dbContext.PracticalAssessmentScores.AddRangeAsync(practicalScores);

        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        Console.WriteLine("Dummy scenario lengkap berhasil dibuat.");
        Console.WriteLine($"Company: {employee.Company?.CompanyName}");
        Console.WriteLine($"Peserta: {employee.Nrp} - {employee.EmployeeName}");
        Console.WriteLine($"Unit: {template.Vehicle?.VehicleName}");
        Console.WriteLine($"Company admin: company.demo.complete / Demo@123");
        Console.WriteLine($"Instructor: instructor.demo.complete / Demo@123");
        Console.WriteLine($"Theory session id: {examSession.Id}");
        Console.WriteLine($"Practical session id: {practicalSession.Id}");
        Console.WriteLine($"Summary URL: /Admin/SummaryScore?theorySessionId={examSession.Id}&practicalSessionId={practicalSession.Id}");
    });
}

static async Task SeedDummyOutcomeMatrixAsync(ApplicationDbContext dbContext)
{
    var strategy = dbContext.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(async () =>
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;

        var templates = await dbContext.PracticalAssessmentTemplates
            .Include(x => x.Items)
            .Include(x => x.Vehicle)
            .Where(x => x.IsActive)
            .OrderBy(x => x.CompanyId)
            .ThenBy(x => x.VehicleId)
            .ToListAsync();

        if (templates.Count == 0)
        {
            throw new InvalidOperationException("Template praktek belum tersedia. Jalankan --reset-masterdata-seed terlebih dahulu.");
        }

        var employeePool = await dbContext.Employees
            .Include(x => x.Company)
            .OrderBy(x => x.CompanyId)
            .ThenBy(x => x.Id)
            .ToListAsync();

        if (employeePool.Count == 0)
        {
            throw new InvalidOperationException("Data employee belum tersedia. Jalankan --reset-masterdata-seed terlebih dahulu.");
        }

        var primaryCompanyId = templates.First().CompanyId;
        var userHasher = new PasswordHasher<UserLogin>();
        var examHasher = new PasswordHasher<ExamSession>();

        var companyAdmin = await EnsureDemoUserAsync(
            dbContext, userHasher, "company.demo.matrix", "Company Demo Matrix", SystemUserRole.CompanyAdmin, primaryCompanyId, "Demo@123");
        var instructor = await EnsureDemoUserAsync(
            dbContext, userHasher, "instructor.demo.matrix", "Instructor Demo Matrix", SystemUserRole.Instructor, primaryCompanyId, "Demo@123");

        var previousTheorySessionIds = await dbContext.ExamSessions
            .Where(x => x.Token.StartsWith("dmx-theory-"))
            .Select(x => x.Id)
            .ToListAsync();
        if (previousTheorySessionIds.Count > 0)
        {
            await dbContext.ExamLogs.Where(x => previousTheorySessionIds.Contains(x.SessionId)).ExecuteDeleteAsync();
            await dbContext.ExamAnswers.Where(x => previousTheorySessionIds.Contains(x.SessionId)).ExecuteDeleteAsync();
            await dbContext.ExamQuestions.Where(x => previousTheorySessionIds.Contains(x.SessionId)).ExecuteDeleteAsync();
            await dbContext.ExamResults.Where(x => previousTheorySessionIds.Contains(x.SessionId)).ExecuteDeleteAsync();
            await dbContext.ExamSessions.Where(x => previousTheorySessionIds.Contains(x.Id)).ExecuteDeleteAsync();
        }

        var previousPracticalSessionIds = await dbContext.PracticalAssessmentSessions
            .Where(x => x.InstructorNote != null && x.InstructorNote.StartsWith("[DMX]"))
            .Select(x => x.Id)
            .ToListAsync();
        if (previousPracticalSessionIds.Count > 0)
        {
            await dbContext.PracticalAssessmentScores.Where(x => previousPracticalSessionIds.Contains(x.SessionId)).ExecuteDeleteAsync();
            await dbContext.PracticalAssessmentSessions.Where(x => previousPracticalSessionIds.Contains(x.Id)).ExecuteDeleteAsync();
        }

        await dbContext.ExamSchedules
            .Where(x => x.CreatedByUserId == companyAdmin.Id && x.Status.StartsWith("dmx-"))
            .ExecuteDeleteAsync();

        var scenarios = new List<DummyOutcomeScenario>
        {
            new("LULUS SEMUA", true, 90, true, true, 88, true),
            new("TEORI LULUS PRAKTEK GAGAL", true, 84, true, true, 62, false),
            new("TEORI GAGAL PRAKTEK LULUS", true, 56, false, true, 86, true),
            new("GAGAL SEMUA", true, 42, false, true, 58, false),
            new("TEORI SAJA (LULUS)", true, 78, true, false, null, null),
            new("PRAKTEK SAJA (LULUS)", false, null, null, true, 83, true),
            new("TEORI PROSES", true, null, null, false, null, null),
            new("PRAKTEK PROSES", false, null, null, true, null, null)
        };

        var createdTheory = 0;
        var createdPractical = 0;
        for (var i = 0; i < scenarios.Count; i++)
        {
            var scenario = scenarios[i];
            var template = templates[i % templates.Count];
            var employee = employeePool.FirstOrDefault(x => x.CompanyId == template.CompanyId) ?? employeePool[i % employeePool.Count];
            var slotTime = now.Date.AddDays(-i).AddHours(8 + (i % 5));

            if (scenario.CreateTheory)
            {
                var examSchedule = new ExamSchedule
                {
                    EmployeeId = employee.Id,
                    VehicleId = template.VehicleId,
                    ScheduledAt = slotTime,
                    Status = scenario.TheoryScore.HasValue ? "completed" : "dmx-in-progress",
                    CreatedByUserId = companyAdmin.Id,
                    CreatedAt = now.AddMinutes(-(i * 9 + 30))
                };
                await dbContext.ExamSchedules.AddAsync(examSchedule);

                var examSession = new ExamSession
                {
                    EmployeeId = employee.Id,
                    VehicleId = template.VehicleId,
                    Token = $"dmx-theory-{Guid.NewGuid():N}",
                    RefId = $"DMX{DateTime.UtcNow:MMdd}{i:000}",
                    AccessPasswordHash = string.Empty,
                    StartTime = scenario.TheoryScore.HasValue ? slotTime : null,
                    EndTime = slotTime.AddMinutes(50),
                    Status = scenario.TheoryScore.HasValue ? ExamSessionStatus.Completed : ExamSessionStatus.Active,
                    CameraActive = true,
                    TabSwitchCount = i % 3,
                    CreatedAt = now.AddMinutes(-(i * 9 + 40))
                };
                examSession.AccessPasswordHash = examHasher.HashPassword(examSession, "Demo123");
                await dbContext.ExamSessions.AddAsync(examSession);
                await dbContext.SaveChangesAsync();

                var questions = await dbContext.Questions
                    .Where(x => x.CompanyId == template.CompanyId && x.VehicleId == template.VehicleId)
                    .OrderBy(x => x.Id)
                    .Take(5)
                    .ToListAsync();

                if (questions.Count < 5)
                {
                    throw new InvalidOperationException($"Soal teori minimal 5 belum tersedia untuk unit {template.Vehicle?.VehicleName}.");
                }

                await dbContext.ExamQuestions.AddRangeAsync(questions.Select((q, idx) => new ExamQuestion
                {
                    SessionId = examSession.Id,
                    QuestionId = q.Id,
                    QuestionOrder = idx + 1
                }));

                if (scenario.TheoryScore.HasValue)
                {
                    var correctCount = scenario.TheoryPass == true ? 4 : 2;
                    await dbContext.ExamAnswers.AddRangeAsync(questions.Select((q, idx) => new ExamAnswer
                    {
                        SessionId = examSession.Id,
                        QuestionId = q.Id,
                        SelectedAnswer = idx < correctCount ? q.CorrectAnswer : (q.CorrectAnswer == "A" ? "B" : "A"),
                        IsCorrect = idx < correctCount,
                        AnsweredAt = slotTime.AddMinutes(8 + idx * 3)
                    }));

                    await dbContext.ExamResults.AddAsync(new ExamResult
                    {
                        SessionId = examSession.Id,
                        TotalQuestions = 5,
                        CorrectAnswers = correctCount,
                        Score = scenario.TheoryScore.Value,
                        PassStatus = scenario.TheoryPass == true,
                        FinishedAt = slotTime.AddMinutes(47)
                    });
                }

                createdTheory++;
            }

            if (scenario.CreatePractical)
            {
                var practicalSession = new PracticalAssessmentSession
                {
                    EmployeeId = employee.Id,
                    VehicleId = template.VehicleId,
                    TemplateId = template.Id,
                    InstructorUserId = instructor.Id,
                    ScheduledAt = slotTime.AddHours(3),
                    Status = scenario.PracticalScore.HasValue ? PracticalAssessmentStatus.Submitted : PracticalAssessmentStatus.Scheduled,
                    FinalNumericScore = scenario.PracticalScore,
                    FinalGrade = null,
                    PassStatus = scenario.PracticalPass,
                    InstructorNote = $"[DMX] {scenario.Name}",
                    CreatedByUserId = companyAdmin.Id,
                    CreatedAt = now.AddMinutes(-(i * 9 + 20)),
                    SubmittedAt = scenario.PracticalScore.HasValue ? slotTime.AddHours(3).AddMinutes(35) : null
                };
                await dbContext.PracticalAssessmentSessions.AddAsync(practicalSession);
                await dbContext.SaveChangesAsync();

                if (scenario.PracticalScore.HasValue)
                {
                    var scoreBase = scenario.PracticalPass == true ? 80 : 58;
                    var practicalScores = template.Items
                        .OrderBy(x => x.DisplayOrder)
                        .Select((item, idx) => new PracticalAssessmentScore
                        {
                            SessionId = practicalSession.Id,
                            TemplateItemId = item.Id,
                            NumericValue = scoreBase + (idx % 2 == 0 ? 4 : -2),
                            Note = $"{scenario.Name} - item {idx + 1}"
                        })
                        .ToList();
                    await dbContext.PracticalAssessmentScores.AddRangeAsync(practicalScores);
                }

                createdPractical++;
            }
        }

        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        Console.WriteLine("Dummy outcome matrix berhasil dibuat.");
        Console.WriteLine($"Theory sessions dibuat: {createdTheory}");
        Console.WriteLine($"Practical sessions dibuat: {createdPractical}");
        Console.WriteLine("Skenario: lulus semua, gagal semua, campuran, teori saja, praktek saja, dan status proses.");
    });
}

static async Task<UserLogin> EnsureDemoUserAsync(
    ApplicationDbContext dbContext,
    PasswordHasher<UserLogin> passwordHasher,
    string username,
    string fullName,
    string role,
    long companyId,
    string password)
{
    var user = await dbContext.UserLogins.FirstOrDefaultAsync(x => x.Username == username);
    if (user is null)
    {
        user = new UserLogin
        {
            Username = username,
            FullName = fullName,
            Role = role,
            CompanyId = companyId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);
        await dbContext.UserLogins.AddAsync(user);
    }
    else
    {
        user.FullName = fullName;
        user.Role = role;
        user.CompanyId = companyId;
        user.IsActive = true;
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        dbContext.UserLogins.Update(user);
    }

    await dbContext.SaveChangesAsync();
    return user;
}

static List<QuestionTemplate> GetQuestionTemplatesByVehicle(string vehicleName)
{
    var key = vehicleName.Trim().ToUpperInvariant();
    return key switch
    {
        "HD 785" => new List<QuestionTemplate>
        {
            new("Sebelum mengoperasikan HD 785, pemeriksaan pra-operasi yang paling wajib adalah ...", "Memastikan rem, lampu, ban, klakson, dan level fluida dalam kondisi normal", "Langsung memuat material maksimal", "Melakukan pemanasan unit sambil berjalan", "Menonaktifkan alarm kabin agar tidak mengganggu", "A", "easy"),
            new("Saat membawa muatan penuh pada jalan menurun, operator HD 785 harus ...", "Menggunakan gigi tinggi agar perjalanan lebih singkat", "Menggunakan engine brake atau retarder dengan kecepatan terkendali", "Memposisikan transmisi netral saat turunan", "Mematikan retarder agar bahan bakar lebih hemat", "B", "medium"),
            new("Jarak aman dengan unit di depan saat hauling minimal adalah ...", "Satu panjang unit", "Dua detik", "Tiga sampai lima detik atau sesuai SOP site", "Tidak ada ketentuan selama jalan lurus", "C", "medium"),
            new("Jika indikator tekanan oli mesin menyala merah saat operasi, tindakan pertama yang benar adalah ...", "Tetap melanjutkan unit sampai loading point", "Menurunkan volume radio kabin", "Menghentikan unit di area aman lalu melapor kepada pengawas", "Menambah kecepatan agar sirkulasi oli meningkat", "C", "hard"),
            new("Posisi parkir HD 785 yang benar setelah selesai operasi adalah ...", "Rem parkir aktif, transmisi netral, dan wheel chock bila diperlukan", "Transmisi maju agar unit siap bergerak kembali", "Mesin tetap hidup tanpa operator", "Parkir pada area tikungan agar dekat jalur keluar", "A", "easy"),
            new("Blind spot utama HD 785 umumnya berada pada ...", "Area dekat sisi samping dan belakang unit", "Hanya area tepat di depan kaca", "Unit tidak memiliki blind spot", "Hanya pada sisi kiri unit", "A", "medium"),
            new("Komunikasi dengan dispatcher saat terjadi keterlambatan cycle time harus ...", "Tidak perlu disampaikan agar radio tetap sepi", "Disampaikan dengan jelas melalui radio sesuai prosedur komunikasi site", "Menunggu sampai dispatcher bertanya lebih dahulu", "Dilaporkan setelah shift selesai", "B", "easy"),
            new("Saat terdapat pejalan kaki memasuki jalur hauling, operator harus ...", "Membunyikan klakson panjang lalu tetap melaju", "Mengurangi kecepatan dan memastikan area benar-benar aman", "Mengambil jalur berlawanan agar tidak terhambat", "Mengabaikan kondisi selama pejalan kaki sudah melihat unit", "B", "hard"),
            new("Tujuan utama pre-start check pada HD 785 adalah ...", "Menambah jam kerja operator", "Memastikan unit laik operasi dan mencegah potensi insiden", "Mengurangi waktu istirahat operator", "Memenuhi formalitas administratif semata", "B", "easy"),
            new("Jika muatan tidak stabil saat proses dumping, tindakan yang benar adalah ...", "Tetap melakukan dumping secepat mungkin", "Memindahkan unit ke area yang lebih miring", "Menghentikan proses, menstabilkan posisi unit, lalu dumping sesuai prosedur", "Meminta helper berdiri di belakang unit sebagai panduan", "C", "hard")
        },
        "LV" => new List<QuestionTemplate>
        {
            new("Sebelum mengemudi LV di area tambang, operator wajib ...", "Menggunakan sabuk pengaman dan memastikan posisi duduk aman", "Menyalakan musik dengan volume tinggi", "Menutup seluruh spion samping", "Mengoperasikan ponsel untuk navigasi manual", "A", "easy"),
            new("Batas kecepatan LV harus mengikuti ...", "Preferensi pengemudi", "Kondisi kendaraan saja", "Rambu lalu lintas dan SOP site", "Kecepatan kendaraan di depan", "C", "easy"),
            new("Saat melewati persimpangan di area tambang, tindakan yang benar adalah ...", "Langsung melaju selama jalan terlihat kosong", "Mengurangi kecepatan, melihat kanan-kiri, dan mematuhi prioritas", "Membunyikan klakson tanpa mengurangi kecepatan", "Menyalip kendaraan lain di area simpang", "B", "medium"),
            new("Jika hujan lebat menyebabkan jarak pandang rendah, operator LV harus ...", "Menjaga kecepatan normal agar tidak terlambat", "Menambah kecepatan agar cepat keluar dari area hujan", "Mengurangi kecepatan dan menyalakan lampu sesuai kebutuhan", "Mematikan lampu agar pantulan tidak mengganggu", "C", "medium"),
            new("Saat memarkir LV di area kerja, posisi yang benar adalah ...", "Mesin tetap hidup dan pintu dibiarkan terbuka", "Rem parkir aktif dan kendaraan berada pada posisi aman", "Parkir di jalur evakuasi agar mudah diakses", "Parkir dekat tikungan buta", "B", "easy"),
            new("Larangan utama saat mengemudi LV adalah ...", "Menggunakan seatbelt", "Fokus pada jalur perjalanan", "Mengoperasikan ponsel tanpa perangkat handsfree", "Mematuhi rambu site", "C", "easy"),
            new("Fungsi safety talk sebelum perjalanan adalah ...", "Menambah durasi briefing tanpa tujuan teknis", "Menyamakan pemahaman risiko perjalanan dan kontrolnya", "Menggantikan seluruh SOP operasional", "Sekadar administrasi tanpa manfaat nyata", "B", "medium"),
            new("Jika menemukan hewan melintas di jalan hauling, operator LV harus ...", "Membunyikan klakson terus menerus sambil tetap melaju", "Berhenti atau mengurangi kecepatan sampai jalur benar-benar aman", "Menyalip melalui bahu jalan", "Mematikan lampu kendaraan", "B", "medium"),
            new("Posisi tangan yang disarankan saat mengemudi adalah ...", "Satu tangan agar lebih santai", "Kedua tangan stabil pada posisi kendali yang aman", "Melepaskan tangan saat jalan lurus", "Satu tangan sambil memegang ponsel", "B", "easy"),
            new("Jika terjadi near miss saat berkendara, operator wajib ...", "Menyembunyikan kejadian agar pekerjaan tidak terganggu", "Melanjutkan kerja tanpa pelaporan", "Melaporkan kejadian sesuai prosedur pelaporan insiden", "Hanya menceritakan ke rekan kerja terdekat", "C", "hard")
        },
        "CRANE TRUCK" => new List<QuestionTemplate>
        {
            new("Sebelum lifting dengan crane truck, langkah wajib adalah ...", "Cek load chart dan kapasitas angkat", "Langsung angkat beban", "Naikkan boom maksimum", "Matikan outrigger", "A", "medium"),
            new("Outrigger pada crane truck berfungsi untuk ...", "Menambah kecepatan jalan", "Menstabilkan unit saat lifting", "Menghemat bahan bakar", "Mempercepat swing", "B", "easy"),
            new("Jika berat beban tidak diketahui pasti, operator harus ...", "Tetap mengangkat dengan perkiraan", "Minta signalman memutuskan", "Verifikasi berat beban sebelum lifting", "Angkat setengah tinggi dulu", "C", "hard"),
            new("Komunikasi saat lifting menggunakan ...", "Isyarat tangan/radio yang disepakati", "Teriakan bebas", "Tidak perlu komunikasi", "Hanya dari operator", "A", "easy"),
            new("Saat angin kencang melebihi batas aman, tindakan benar ...", "Tetap operasi agar target tercapai", "Hentikan lifting sampai kondisi aman", "Kurangi operator di area", "Tambah kecepatan swing", "B", "hard"),
            new("Zona larangan berdiri saat lifting adalah ...", "Di bawah beban gantung", "Di belakang operator", "Di dalam kabin crane", "Di area parkir", "A", "easy"),
            new("Sebelum boom digerakkan, area sekitar harus ...", "Dipastikan clear dari personel dan obstacle", "Dipakai untuk istirahat", "Diabaikan jika cepat", "Ditutup sebagian", "A", "medium"),
            new("Fungsi signalman/rigger dalam operasi crane adalah ...", "Menggantikan operator", "Mengarahkan lifting dan memastikan keamanan area", "Mencatat jam kerja", "Mengatur bahan bakar", "B", "medium"),
            new("Jika alarm overload berbunyi, operator harus ...", "Mengabaikan dan lanjut", "Turunkan beban dan evaluasi kondisi", "Mematikan alarm", "Mempercepat pengangkatan", "B", "hard"),
            new("Dokumen izin kerja lifting diperlukan untuk ...", "Formalitas administrasi", "Kontrol risiko dan verifikasi kesiapan kerja", "Menggantikan SOP", "Mengurangi personel", "B", "medium")
        },
        "PC 200" => GetExcavatorTemplates("PC 200"),
        "PC 1000" => GetExcavatorTemplates("PC 1000"),
        "PC 2000" => GetExcavatorTemplates("PC 2000"),
        _ => GetExcavatorTemplates(vehicleName)
    };
}

static List<QuestionTemplate> GetExcavatorTemplates(string unitName)
{
    return new List<QuestionTemplate>
    {
        new($"Pemeriksaan utama sebelum operasi {unitName} adalah ...", "Memastikan kondisi track, attachment, serta tidak ada kebocoran fluida", "Mengisi bucket penuh sebelum inspeksi", "Langsung melakukan warming up di area sempit", "Menonaktifkan alarm unit", "A", "easy"),
        new($"Saat melakukan swing dengan {unitName}, operator harus ...", "Memastikan area swing clear dan pergerakan tetap terkendali", "Melakukan swing secepat mungkin untuk mengejar target", "Mengayunkan bucket di atas kabin dump truck", "Mengabaikan signalman selama jarak cukup jauh", "A", "medium"),
        new($"Posisi aman saat loading dump truck dengan {unitName} adalah ...", "Bucket melintas di atas kabin truck", "Menjaga sudut dan tinggi kerja aman sesuai SOP", "Menjatuhkan material dari belakang kabin truck", "Membiarkan truck bergerak saat loading", "B", "medium"),
        new($"Jika terdengar suara abnormal pada sistem hidrolik {unitName}, operator wajib ...", "Tetap melanjutkan operasi sampai shift selesai", "Menghentikan unit di area aman dan melaporkan untuk inspeksi", "Menaikkan rpm agar tekanan stabil", "Mengurangi oli tanpa pemeriksaan", "B", "hard"),
        new($"Saat bekerja dekat tepi jenjang (edge), {unitName} harus ...", "Mendekat semaksimal mungkin agar jangkauan lebih jauh", "Menjaga jarak aman dari edge sesuai kondisi lapangan", "Memposisikan track miring ke arah tepi", "Beroperasi tanpa spotter", "B", "hard"),
        new($"Tujuan warming up unit {unitName} sebelum bekerja adalah ...", "Memastikan sistem pelumasan dan hidrolik mencapai kondisi stabil", "Menghemat jam kerja operator", "Meningkatkan kebisingan area kerja", "Mempercepat pendinginan kabin", "A", "easy"),
        new($"Komunikasi operator {unitName} dengan dump truck saat antrean loading harus ...", "Dilakukan bebas tanpa prosedur", "Mengikuti prosedur komunikasi site yang berlaku", "Cukup menggunakan klakson sekali", "Tidak diperlukan bila area terlihat aman", "B", "easy"),
        new($"Saat visibilitas terganggu oleh debu, operator {unitName} harus ...", "Tetap bekerja dengan pola normal", "Mengurangi aktivitas dan memastikan jarak aman tetap terjaga", "Meminta truck mendekat agar lebih cepat", "Mematikan lampu kerja", "B", "medium"),
        new($"Parkir aman {unitName} setelah operasi adalah ...", "Attachment diturunkan, rem parkir aktif, dan mesin dimatikan sesuai SOP", "Membiarkan attachment menggantung", "Meninggalkan mesin hidup semalaman", "Meninggalkan unit tanpa pengamanan", "A", "easy"),
        new($"Jika ada personel masuk radius kerja {unitName}, tindakan yang benar adalah ...", "Melanjutkan operasi dengan kecepatan rendah", "Menghentikan seluruh gerakan sampai area benar-benar aman", "Membunyikan klakson lalu tetap bekerja", "Meminta personel menunggu di bucket", "B", "hard")
    };
}

static List<PracticalTemplateSeedItem> GetPracticalTemplateDefinitions(string templateName)
{
    var key = templateName.Trim().ToUpperInvariant();
    if (key.Contains("LV"))
    {
        return new List<PracticalTemplateSeedItem>
        {
            new("Persiapan", "Pengecekan kelengkapan dokumen dan APD", 20),
            new("Operasional", "Kontrol kendaraan dan kepatuhan jalur", 30),
            new("Safety", "Penerapan defensive driving", 30),
            new("Penutupan", "Parkir aman dan komunikasi akhir", 20)
        };
    }

    if (key.Contains("CRANE"))
    {
        return new List<PracticalTemplateSeedItem>
        {
            new("Persiapan", "Pre-start dan pemeriksaan load chart", 20),
            new("Lifting", "Stabilisasi unit dan penggunaan outrigger", 30),
            new("Kontrol Beban", "Komunikasi dengan signalman/rigger", 25),
            new("Safety", "Pengendalian area larangan dan shutdown aman", 25)
        };
    }

    return new List<PracticalTemplateSeedItem>
    {
        new("Persiapan", "Pre-start inspection unit", 25),
        new("Operasional", "Kontrol unit saat manuver dan positioning", 35),
        new("Produktivitas", "Teknik kerja sesuai SOP site", 20),
        new("Safety", "Kepatuhan keselamatan dan shutdown aman", 20)
    };
}

sealed record QuestionTemplate(
    string QuestionText,
    string OptionA,
    string OptionB,
    string OptionC,
    string OptionD,
    string CorrectAnswer,
    string Difficulty);

sealed record PracticalTemplateSeedItem(
    string SectionName,
    string ItemLabel,
    decimal Weight);

sealed record DummyOutcomeScenario(
    string Name,
    bool CreateTheory,
    decimal? TheoryScore,
    bool? TheoryPass,
    bool CreatePractical,
    decimal? PracticalScore,
    bool? PracticalPass);
