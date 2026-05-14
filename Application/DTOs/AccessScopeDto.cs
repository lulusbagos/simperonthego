namespace SimperSecureOnlineTestSystem.Application.DTOs;

public record AccessScopeDto(bool IsAdministrator, long? CompanyId)
{
    public bool HasCompanyScope => CompanyId.HasValue;
}
