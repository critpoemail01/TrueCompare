using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrueCompare.Data;

namespace TrueCompare.Controllers;

[AllowAnonymous]
public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IConfiguration configuration) : Controller
{
    [HttpPost("/auth/login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromForm] LoginForm input)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
        {
            return RedirectWithMessage("/login", input.ReturnUrl, "Preenche email e password.");
        }

        var result = await signInManager.PasswordSignInAsync(input.Email, input.Password, input.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return RedirectWithMessage("/login", input.ReturnUrl, "Credenciais inválidas.");
        }

        return RedirectToLocal(input.ReturnUrl);
    }

    [HttpPost("/auth/register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromForm] RegisterForm input)
    {
        if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
        {
            return RedirectWithMessage("/register", input.ReturnUrl, "Preenche email e password.");
        }

        if (!string.Equals(input.Password, input.ConfirmPassword, StringComparison.Ordinal))
        {
            return RedirectWithMessage("/register", input.ReturnUrl, "As passwords não coincidem.");
        }

        var user = new ApplicationUser
        {
            UserName = input.Email.Trim(),
            Email = input.Email.Trim(),
            EmailConfirmed = true
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

    [HttpPost("/auth/logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Redirect("/");
    }

    [HttpGet("/auth/google")]
    public IActionResult Google([FromQuery] string? returnUrl = null)
    {
        if (!GoogleConfigured())
        {
            return RedirectWithMessage("/login", returnUrl, "Login por Gmail ainda não configurado. Define Authentication:Google:ClientId e ClientSecret.");
        }

        var redirectUrl = Url.Action(nameof(GoogleCallback), "Account", new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
        return Challenge(properties, "Google");
    }

    [HttpGet("/auth/google-callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl = null)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            return RedirectWithMessage("/login", returnUrl, "Não foi possível ler a resposta da Google.");
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
            return RedirectWithMessage("/login", returnUrl, "A conta Google não devolveu email.");
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
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
