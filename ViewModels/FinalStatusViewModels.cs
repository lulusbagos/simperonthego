namespace SimperSecureOnlineTestSystem.ViewModels;

public class FinalStatusViewModel
{
    public bool IsAdministrator { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int TotalRows { get; set; }
    public int CompleteRows { get; set; }
    public int PassedRows { get; set; }
    public int FailedRows { get; set; }
    public int InProgressRows { get; set; }
    public List<FinalStatusRowViewModel> Rows { get; set; } = new();
}

public class FinalStatusRowViewModel
{
    public string Nrp { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public long? VehicleId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public decimal? TheoryScore { get; set; }
    public bool? TheoryPassStatus { get; set; }
    public DateTime? TheoryFinishedAt { get; set; }
    public long? TheorySessionId { get; set; }
    public decimal? PracticalNumericScore { get; set; }
    public string? PracticalGrade { get; set; }
    public bool? PracticalPassStatus { get; set; }
    public DateTime? PracticalFinishedAt { get; set; }
    public long? PracticalSessionId { get; set; }
    public string? InstructorName { get; set; }
    public DateTime? LatestActivityAt =>
        new[] { TheoryFinishedAt, PracticalFinishedAt }
            .Where(x => x.HasValue)
            .OrderByDescending(x => x)
            .FirstOrDefault();

    public string FinalStatus
    {
        get
        {
            if (TheoryPassStatus == true && PracticalPassStatus == true)
            {
                return "LULUS";
            }

            if (TheoryPassStatus == false || PracticalPassStatus == false)
            {
                return "TIDAK LULUS";
            }

            if (TheoryPassStatus.HasValue || PracticalPassStatus.HasValue)
            {
                return "PROSES";
            }

            return "BELUM MULAI";
        }
    }
}
