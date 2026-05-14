namespace SimperSecureOnlineTestSystem.Application.DTOs;

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthUserDto
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public long? CompanyId { get; set; }
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public long? CompanyId { get; set; }
}

public class UpdateUserDto
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public long? CompanyId { get; set; }
}

public class ResetUserPasswordDto
{
    public long UserId { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}

public class CompanyUpsertDto
{
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

public class ProfilePasswordDto
{
    public long UserId { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
