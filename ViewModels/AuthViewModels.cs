using System.ComponentModel.DataAnnotations;
using SimperSecureOnlineTestSystem.Domain.Entities;

namespace SimperSecureOnlineTestSystem.ViewModels;

public class LoginViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class UserManageViewModel
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public long? CompanyId { get; set; }

    public List<UserLogin> ExistingUsers { get; set; } = new();
    public List<Company> ExistingCompanies { get; set; } = new();
}

public class UserEditViewModel
{
    [Required]
    public long UserId { get; set; }

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public long? CompanyId { get; set; }
}

public class UserResetPasswordViewModel
{
    [Required]
    public long UserId { get; set; }

    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }
}
