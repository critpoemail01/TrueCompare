using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrueCompare.Data;
using TrueCompare.Services;

namespace TrueCompare.Controllers;

public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IConfiguration configuration,
    AppText text) : Controller
{
    [AllowAnonymous]
    [HttpPost("/auth/login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromForm] LoginForm input)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
        {
            return RedirectWithMessage("/login", input.ReturnUrl, text.Pick("Preenche email e password.", "Enter email and password."));
        }

        var user = await userManager.FindByEmailAsync(input.Email.Trim());
        if (user is null)
        {
            return RedirectWithMessage("/login", input.ReturnUrl, text.Pick("Credenciais inválidas.", "Invalid credentials."));
        }

        var result = await signInManager.PasswordSignInAsync(user, input.Password, input.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return RedirectWithMessage("/login", input.ReturnUrl, text.Pick("Credenciais inválidas.", "Invalid credentials."));
        }

        return RedirectToLocal(input.ReturnUrl);
    }

    [AllowAnonymous]
    [HttpPost("/auth/register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromForm] RegisterForm input)
    {
        if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
        {
            return RedirectWithMessage("/register", input.ReturnUrl, text.Pick("Preenche email e password.", "Enter email and password."));
        }

        if (!string.Equals(input.Password, input.ConfirmPassword, StringComparison.Ordinal))
        {
            return RedirectWithMessage("/register", input.ReturnUrl, text.Pick("As passwords não coincidem.", "Passwords do not match."));
        }

        var user = new ApplicationUser
        {
            UserName = input.Email.Trim(),
            Email = input.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = BuildDefaultDisplayName(input.Email)
        };

        var result = await userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            var error = string.Join(" ", result.Errors.Select(error => error.Description));
            return RedirectWithMessage("/register", input.ReturnUrl, error);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToLocal(input.ReturnUrl);
    }

    [HttpPost("/auth/settings/profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromForm] ProfileForm input)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var email = input.Email?.Trim();
        var displayName = string.IsNullOrWhiteSpace(input.DisplayName)
            ? null
            : input.DisplayName.Trim();

        if (string.IsNullOrWhiteSpace(email) || !LooksLikeEmail(email))
        {
            return RedirectToSettings(text.Pick("Indica um email válido.", "Enter a valid email."));
        }

        if (displayName?.Length > 120)
        {
            return RedirectToSettings(text.Pick("O nome deve ter no máximo 120 caracteres.", "Name must be 120 characters or fewer."));
        }

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null && !string.Equals(existingUser.Id, user.Id, StringComparison.Ordinal))
        {
            return RedirectToSettings(text.Pick("Esse email já está associado a outra conta.", "That email is already associated with another account."));
        }

        user.DisplayName = displayName;
        user.Email = email;
        user.UserName = email;
        user.NormalizedEmail = userManager.NormalizeEmail(email);
        user.NormalizedUserName = userManager.NormalizeName(email);
        user.EmailConfirmed = true;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return RedirectToSettings(string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        await signInManager.RefreshSignInAsync(user);
        return RedirectToSettings(text.Pick("Definições atualizadas.", "Settings updated."));
    }

    [HttpPost("/auth/settings/password")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordForm input)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (string.IsNullOrWhiteSpace(input.NewPassword))
        {
            return RedirectToSettings(text.Pick("Indica a nova password.", "Enter the new password."));
        }

        if (!string.Equals(input.NewPassword, input.ConfirmNewPassword, StringComparison.Ordinal))
        {
            return RedirectToSettings(text.Pick("As passwords não coincidem.", "Passwords do not match."));
        }

        var hasPassword = await userManager.HasPasswordAsync(user);
        var result = hasPassword
            ? await userManager.ChangePasswordAsync(user, input.CurrentPassword ?? string.Empty, input.NewPassword)
            : await userManager.AddPasswordAsync(user, input.NewPassword);

        if (!result.Succeeded)
        {
            return RedirectToSettings(string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        await signInManager.RefreshSignInAsync(user);
        return RedirectToSettings(text.Pick("Password alterada.", "Password changed."));
    }

    [HttpPost("/auth/logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Redirect("/");
    }

    [AllowAnonymous]
    [HttpGet("/auth/google")]
    public IActionResult Google([FromQuery] string? returnUrl = null)
    {
        if (!GoogleConfigured())
        {
            return RedirectWithMessage("/login", returnUrl, text.Pick("Login por Gmail ainda não configurado. Define Authentication:Google:ClientId e ClientSecret.", "Gmail login is not configured yet. Set Authentication:Google:ClientId and ClientSecret."));
        }

        var redirectUrl = Url.Action(nameof(GoogleCallback), "Account", new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
        return Challenge(properties, "Google");
    }

    [AllowAnonymous]
    [HttpGet("/auth/google-callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl = null)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            return RedirectWithMessage("/login", returnUrl, text.Pick("Não foi possível ler a resposta da Google.", "Could not read Google's response."));
        }

        var loginResult = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (loginResult.Succeeded)
        {
            return RedirectToLocal(returnUrl);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectWithMessage("/login", returnUrl, text.Pick("A conta Google não devolveu email.", "The Google account did not return an email."));
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? BuildDefaultDisplayName(email)
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var error = string.Join(" ", createResult.Errors.Select(error => error.Description));
                return RedirectWithMessage("/login", returnUrl, error);
            }
        }

        var addLoginResult = await userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded && addLoginResult.Errors.All(error => error.Code != "LoginAlreadyAssociated"))
        {
            var error = string.Join(" ", addLoginResult.Errors.Select(error => error.Description));
            return RedirectWithMessage("/login", returnUrl, error);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToLocal(returnUrl);
    }

    private bool GoogleConfigured()
    {
        return !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"])
            && !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]);
    }

    private IActionResult RedirectWithMessage(string path, string? returnUrl, string message)
    {
        var url = $"{path}?message={Uri.EscapeDataString(message)}";
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            url += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
        }

        return Redirect(url);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }

    private IActionResult RedirectToSettings(string message)
    {
        return Redirect($"/settings?message={Uri.EscapeDataString(message)}");
    }

    private static string? BuildDefaultDisplayName(string? email)
    {
        var localPart = email?.Split('@', 2)[0].Trim();
        return string.IsNullOrWhiteSpace(localPart) ? null : localPart;
    }

    private static bool LooksLikeEmail(string value)
    {
        try
        {
            var address = new System.Net.Mail.MailAddress(value);
            return string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

public sealed class LoginForm
{
    public string? Email { get; set; }

    public string? Password { get; set; }

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterForm
{
    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? ConfirmPassword { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class ProfileForm
{
    public string? DisplayName { get; set; }

    public string? Email { get; set; }
}

public sealed class ChangePasswordForm
{
    public string? CurrentPassword { get; set; }

    public string? NewPassword { get; set; }

    public string? ConfirmNewPassword { get; set; }
}
