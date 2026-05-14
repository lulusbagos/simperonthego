using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimperSecureOnlineTestSystem.Application.DTOs;
using SimperSecureOnlineTestSystem.Application.Services;
using SimperSecureOnlineTestSystem.Domain.Enums;
using SimperSecureOnlineTestSystem.ViewModels;

namespace SimperSecureOnlineTestSystem.Controllers;

public class AccountController : Controller
{
    private readonly IAdminService _adminService;

    public AccountController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectAfterLogin(User.FindFirstValue(ClaimTypes.Role));
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var authUser = await _adminService.ValidateUserAsync(new LoginRequestDto
        {
            Username = model.Username,
            Password = model.Password
        }, cancellationToken);

        if (authUser is null)
        {
            ModelState.AddModelError(string.Empty, "Username atau password salah.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, authUser.UserId.ToString()),
            new(ClaimTypes.Name, authUser.FullName),
            new(ClaimTypes.Role, authUser.Role),
            new("username", authUser.Username)
        };

        if (authUser.CompanyId.HasValue)
        {
            claims.Add(new Claim("company_id", authUser.CompanyId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return RedirectAfterLogin(authUser.Role);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _adminService.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new ProfileManageViewModel
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            CompanyId = user.CompanyId
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileManageViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _adminService.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        model.UserId = user.Id;
        model.Username = user.Username;
        model.FullName = user.FullName;
        model.Role = user.Role;
        model.CompanyId = user.CompanyId;

        if (string.IsNullOrWhiteSpace(model.CurrentPassword) || string.IsNullOrWhiteSpace(model.NewPassword))
        {
            ModelState.AddModelError(string.Empty, "Isi current password dan new password.");
            return View(model);
        }

        try
        {
            await _adminService.ChangeOwnPasswordAsync(new ProfilePasswordDto
            {
                UserId = userId,
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword
            }, cancellationToken);

            TempData["ProfileSuccess"] = "Password berhasil diubah.";
            return RedirectToAction(nameof(Profile));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private bool TryGetCurrentUserId(out long userId)
    {
        userId = 0;
        var idText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(idText, out userId);
    }

    private IActionResult RedirectAfterLogin(string? role)
    {
        if (role == SystemUserRole.Instructor)
        {
            return RedirectToAction("MyAssignments", "Practical");
        }

        return RedirectToAction("Dashboard", "Admin");
    }
}
