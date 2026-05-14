using SimperSecureOnlineTestSystem.Domain.Entities;

namespace SimperSecureOnlineTestSystem.ViewModels;

public class PermitHaulingLookupViewModel
{
    public string? ReqId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PermitHaulingDetailViewModel
{
    public string ReqId { get; set; } = string.Empty;
    public PermitApprovalView? Permit { get; set; }
    public bool IsHaulingCompleted { get; set; }
    public string? CertificateId { get; set; }
    public string? CertificateUrl { get; set; }
    public string? CertificateQrDataUrl { get; set; }
    public DateTimeOffset? CertifiedAt { get; set; }
    public string? CompanyLogoPath { get; set; }
    public string? AppLogoPath { get; set; }
    public bool SourceUnavailable { get; set; }
    public string? ErrorMessage { get; set; }
}
