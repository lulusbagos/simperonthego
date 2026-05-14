namespace SimperSecureOnlineTestSystem.Application.DTOs;

public class ScheduleBoardDto
{
    public DateTime Date { get; set; }
    public bool IsAdministrator { get; set; }
    public long? ScopedCompanyId { get; set; }
    public int MaxParticipantsPerSession { get; set; }
    public List<ScheduleEmployeeDto> Employees { get; set; } = new();
    public List<ScheduleEmployeeDto> DisplayedEmployees { get; set; } = new();
    public List<ScheduleVehicleDto> Vehicles { get; set; } = new();
    public List<ScheduleItemDto> Items { get; set; } = new();
    public List<string> PracticalCompletedKeys { get; set; } = new();
    public int EmployeePage { get; set; } = 1;
    public int EmployeePageSize { get; set; } = 40;
    public int TotalEmployeeCount { get; set; }
    public int TotalEmployeePages => EmployeePageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling((double)TotalEmployeeCount / EmployeePageSize));
}

public class ScheduleEmployeeDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Nik { get; set; } = string.Empty;
    public string Ktp { get; set; } = string.Empty;
    public string Nrp { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
}

public class ScheduleVehicleDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public string SimperType { get; set; } = string.Empty;
}

public class ScheduleItemDto
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public long VehicleId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Nrp { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ScheduleAssignRequestDto
{
    public long EmployeeId { get; set; }
    public long VehicleId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public long? CreatedByUserId { get; set; }
}

public class ScheduleRemoveRequestDto
{
    public long ScheduleId { get; set; }
}

public class ScheduleMoveRequestDto
{
    public long ScheduleId { get; set; }
    public long VehicleId { get; set; }
    public DateTime ScheduledAt { get; set; }
}

public class ScheduleGenerateAccessRequestDto
{
    public long EmployeeId { get; set; }
    public long VehicleId { get; set; }
}

public class AutoScheduleRequestDto
{
    public DateTime StartDate { get; set; }
    public int PeoplePerDay { get; set; }
}
