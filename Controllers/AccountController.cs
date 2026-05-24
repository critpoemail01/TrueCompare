using System.Security.Claims;
using System.Text.Encodings.Web;
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
    IEmailSender emailSender,
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
            if (result.IsNotAllowed
                && !user.EmailConfirmed
                && await userManager.CheckPasswordAsync(user, input.Password))
            {
                var sent = await SendEmailConfirmationAsync(user, input.ReturnUrl);
                return RedirectWithMessage(
                    "/login",
                    input.ReturnUrl,
                    sent
                        ? text.Pick("Confirma o email antes de iniciares sessão. Enviámos um novo link.", "Confirm your email before signing in. We sent a new link.")
                        : text.Pick("Confirma o email antes de iniciares sessão.", "Confirm your email before signing in."));
            }

            return RedirectWithMessage("/login", input.ReturnUrl, text.Pick("Credenciais inválidas.", "Invalid credentials."));
        }

        return RedirectToLocal(input.ReturnUrl);
    }

    [AllowAnonymous]
    [HttpPost("/auth/register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromForm] RegisterForm input)
    {
        var email = input.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(input.Password))
        {
            return RedirectWithMessage("/register", input.ReturnUrl, text.Pick("Preenche email e password.", "Enter email and password."));
        }

        if (!LooksLikeEmail(email))
        {
            return RedirectWithMessage("/register", input.ReturnUrl, text.Pick("Indica um email válido.", "Enter a valid email."));
        }

        if (!string.Equals(input.Password, input.ConfirmPassword, StringComparison.Ordinal))
        {
            return RedirectWithMessage("/register", input.ReturnUrl, text.Pick("As passwords não coincidem.", "Passwords do not match."));
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = AutoConfirmLocalAccounts(),
            DisplayName = BuildDefaultDisplayName(email)
        };

        var result = await userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            var error = string.Join(" ", result.Errors.Select(error => error.Description));
            return RedirectWithMessage("/register", input.ReturnUrl, error);
        }

        if (!user.EmailConfirmed)
        {
            var sent = await SendEmailConfirmationAsync(user, input.ReturnUrl);
            return RedirectWithMessage(
                "/login",
                input.ReturnUrl,
                sent
                    ? text.Pick(
                        "Conta criada. Enviámos um link para confirmares o email antes de iniciares sessão.",
                        "Account created. We sent a link to confirm your email before signing in.")
                    : text.Pick(
                        "Conta criada, mas o envio do email de confirmação falhou. Confirma a configuração SMTP antes de produção.",
                        "Account created, but the confirmation email could not be sent. Check SMTP configuration before production."));
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToLocal(input.ReturnUrl);
    }

    [HttpPost("/auth/settings/profile")]
    [Authorize]
    [EnableRateLimiting("auth")]
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

        var emailChanged = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
        if (emailChanged && await userManager.HasPasswordAsync(user))
        {
            var passwordConfirmed = await userManager.CheckPasswordAsync(user, input.CurrentPassword ?? string.Empty);
            if (!passwordConfirmed)
            {
                return RedirectToSettings(text.Pick("Confirma a password atual para alterar o email.", "Confirm your current password to change the email."));
            }
        }

        user.DisplayName = displayName;

        if (emailChanged && !AutoConfirmEmailChanges())
        {
            var profileResult = await userManager.UpdateAsync(user);
            if (!profileResult.Succeeded)
            {
                return RedirectToSettings(string.Join(" ", profileResult.Errors.Select(error => error.Description)));
            }

            var sent = await SendEmailChangeConfirmationAsync(user, email);
            await signInManager.RefreshSignInAsync(user);
            return RedirectToSettings(sent
                ? text.Pick(
                    "Definições atualizadas. Enviámos um link para confirmares o novo email; o email atual mantém-se ativo até confirmares.",
                    "Settings updated. We sent a link to confirm the new email; your current email remains active until confirmation.")
                : text.Pick(
                    "Definições atualizadas, mas o envio do link para alterar email falhou. O email atual mantém-se ativo.",
                    "Settings updated, but the email-change link failed to send. Your current email remains active."));
        }

        if (emailChanged)
        {
            user.Email = email;
            user.UserName = email;
            user.NormalizedEmail = userManager.NormalizeEmail(email);
            user.NormalizedUserName = userManager.NormalizeName(email);
            user.EmailConfirmed = true;
        }

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

    [AllowAnonymous]
    [HttpPost("/auth/forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword([FromForm] ForgotPasswordForm input)
    {
        var genericMessage = text.Pick(
            "Se esse email existir e estiver confirmado, enviámos um link para redefinir a password.",
            "If that email exists and is confirmed, we sent a password reset link.");

        var email = input.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || !LooksLikeEmail(email))
        {
            return RedirectWithMessage("/forgot-password", null, genericMessage);
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is not null && user.EmailConfirmed)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action(
                nameof(ResetPasswordPage),
                "Account",
                new { userId = user.Id, token },
                Request.Scheme);

            if (!string.IsNullOrWhiteSpace(callbackUrl))
            {
                var safeUrl = HtmlEncoder.Default.Encode(callbackUrl);
                var safeName = HtmlEncoder.Default.Encode(user.DisplayName ?? user.Email ?? email);
                var body = $"""
                    <p>Olá {safeName},</p>
                    <p>Recebemos um pedido para redefinir a tua password TrueCompare.</p>
                    <p><a href="{safeUrl}">Redefinir password</a></p>
                    <p>Se não pediste esta alteração, ignora este email.</p>
                    """;

                try
                {
                    await emailSender.SendAsync(
                        email,
                        text.Pick("Redefinir password TrueCompare", "Reset your TrueCompare password"),
                        body,
                        HttpContext.RequestAborted);
                }
                catch
                {
                    // Keep the response non-enumerating even when SMTP is unavailable.
                }
            }
        }

        return RedirectWithMessage("/login", null, genericMessage);
    }

    [AllowAnonymous]
    [HttpGet("/auth/reset-password")]
    public IActionResult ResetPasswordPage([FromQuery] string? userId, [FromQuery] string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return RedirectWithMessage("/login", null, text.Pick("Link de redefinição inválido.", "Invalid password reset link."));
        }

        return Redirect($"/reset-password?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}");
    }

    [AllowAnonymous]
    [HttpPost("/auth/reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordForm input)
    {
        if (string.IsNullOrWhiteSpace(input.UserId)
            || string.IsNullOrWhiteSpace(input.Token)
            || string.IsNullOrWhiteSpace(input.Password))
        {
            return RedirectWithMessage("/reset-password", null, text.Pick("Preenche a nova password.", "Enter the new password."));
        }

        if (!string.Equals(input.Password, input.ConfirmPassword, StringComparison.Ordinal))
        {
            var returnPath = $"/reset-password?userId={Uri.EscapeDataString(input.UserId)}&token={Uri.EscapeDataString(input.Token)}";
            return RedirectWithMessage(returnPath, null, text.Pick("As passwords não coincidem.", "Passwords do not match."));
        }

        var user = await userManager.FindByIdAsync(input.UserId);
        if (user is null)
        {
            return RedirectWithMessage("/login", null, text.Pick("Link de redefinição inválido.", "Invalid password reset link."));
        }

        var result = await userManager.ResetPasswordAsync(user, input.Token, input.Password);
        if (!result.Succeeded)
        {
            var returnPath = $"/reset-password?userId={Uri.EscapeDataString(input.UserId)}&token={Uri.EscapeDataString(input.Token)}";
            return RedirectWithMessage(returnPath, null, string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        return RedirectWithMessage("/login", null, text.Pick("Password redefinida. Já podes iniciar sessão.", "Password reset. You can now sign in."));
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

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectWithMessage("/login", returnUrl, text.Pick("A conta Google não devolveu email.", "The Google account did not return an email."));
        }

        var googleEmailVerified = IsGoogleEmailVerified(info.Principal);
        if (RequireConfirmedEmail() && !googleEmailVerified)
        {
            return RedirectWithMessage(
                "/login",
                returnUrl,
                text.Pick(
                    "A Google não confirmou este email. Usa uma conta com email verificado.",
                    "Google did not confirm this email. Use an account with a verified email."));
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

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = googleEmailVerified || !RequireConfirmedEmail(),
                DisplayName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? BuildDefaultDisplayName(email)
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var error = string.Join(" ", createResult.Errors.Select(error => error.Description));
                return RedirectWithMessage("/login", returnUrl, error);
            }
        }
        else if (googleEmailVerified && !user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var error = string.Join(" ", updateResult.Errors.Select(error => error.Description));
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

    [AllowAnonymous]
    [HttpGet("/auth/confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string? userId, [FromQuery] string? token, [FromQuery] string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return RedirectWithMessage("/login", returnUrl, text.Pick("Link de confirmação inválido.", "Invalid confirmation link."));
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return RedirectWithMessage("/login", returnUrl, text.Pick("Conta não encontrada.", "Account not found."));
        }

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return RedirectWithMessage("/login", returnUrl, text.Pick("Não foi possível confirmar o email.", "Could not confirm the email."));
        }

        return RedirectWithMessage("/login", returnUrl, text.Pick("Email confirmado. Já podes iniciar sessão.", "Email confirmed. You can now sign in."));
    }

    [AllowAnonymous]
    [HttpGet("/auth/confirm-email-change")]
    public async Task<IActionResult> ConfirmEmailChange([FromQuery] string? userId, [FromQuery] string? email, [FromQuery] string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            return RedirectToSettings(text.Pick("Link de alteração de email inválido.", "Invalid email-change link."));
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return RedirectToSettings(text.Pick("Conta não encontrada.", "Account not found."));
        }

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null && !string.Equals(existingUser.Id, user.Id, StringComparison.Ordinal))
        {
            return RedirectToSettings(text.Pick("Esse email já está associado a outra conta.", "That email is already associated with another account."));
        }

        var changeResult = await userManager.ChangeEmailAsync(user, email, token);
        if (!changeResult.Succeeded)
        {
            return RedirectToSettings(text.Pick("Não foi possível confirmar o novo email.", "Could not confirm the new email."));
        }

        var userNameResult = await userManager.SetUserNameAsync(user, email);
        if (!userNameResult.Succeeded)
        {
            return RedirectToSettings(string.Join(" ", userNameResult.Errors.Select(error => error.Description)));
        }

        return RedirectToSettings(text.Pick("Email alterado e confirmado.", "Email changed and confirmed."));
    }

    private async Task<bool> SendEmailChangeConfirmationAsync(ApplicationUser user, string newEmail)
    {
        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var callbackUrl = Url.Action(
            nameof(ConfirmEmailChange),
            "Account",
            new { userId = user.Id, email = newEmail, token },
            Request.Scheme);
        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            return false;
        }

        var safeUrl = HtmlEncoder.Default.Encode(callbackUrl);
        var safeName = HtmlEncoder.Default.Encode(user.DisplayName ?? user.Email ?? newEmail);
        var body = $"""
            <p>Olá {safeName},</p>
            <p>Confirma este novo email para a tua conta TrueCompare.</p>
            <p><a href="{safeUrl}">Confirmar novo email</a></p>
            <p>Se não pediste esta alteração, ignora este email.</p>
            """;

        return await emailSender.SendAsync(
            newEmail,
            text.Pick("Confirma o novo email TrueCompare", "Confirm your new TrueCompare email"),
            body,
            HttpContext.RequestAborted);
    }

    private async Task<bool> SendEmailConfirmationAsync(ApplicationUser user, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return false;
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var callbackUrl = Url.Action(
            nameof(ConfirmEmail),
            "Account",
            new { userId = user.Id, token, returnUrl },
            Request.Scheme);
        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            return false;
        }

        var safeUrl = HtmlEncoder.Default.Encode(callbackUrl);
        var safeName = HtmlEncoder.Default.Encode(user.DisplayName ?? user.Email);
        var body = $"""
            <p>Olá {safeName},</p>
            <p>Confirma o teu email para ativares a conta TrueCompare.</p>
            <p><a href="{safeUrl}">Confirmar email</a></p>
            <p>Se não criaste esta conta, ignora este email.</p>
            """;

        return await emailSender.SendAsync(
            user.Email,
            text.Pick("Confirma o teu email TrueCompare", "Confirm your TrueCompare email"),
            body,
            HttpContext.RequestAborted);
    }

    private bool RequireConfirmedEmail()
    {
        return configuration.GetValue("Security:RequireConfirmedEmail", false);
    }

    private static bool IsGoogleEmailVerified(ClaimsPrincipal principal)
    {
        return principal.Claims.Any(claim =>
            (claim.Type.Equals("email_verified", StringComparison.OrdinalIgnoreCase)
                || claim.Type.Equals("urn:google:email_verified", StringComparison.OrdinalIgnoreCase))
            && claim.Value.Equals("true", StringComparison.OrdinalIgnoreCase));
    }

    private bool AutoConfirmLocalAccounts()
    {
        return configuration.GetValue("Security:AutoConfirmLocalAccounts", true);
    }

    private bool AutoConfirmEmailChanges()
    {
        return configuration.GetValue("Security:AutoConfirmEmailChanges", true);
    }

    private bool GoogleConfigured()
    {
        return !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"])
            && !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]);
    }

    private IActionResult RedirectWithMessage(string path, string? returnUrl, string message)
    {
        var separator = path.Contains('?') ? '&' : '?';
        var url = $"{path}{separator}message={Uri.EscapeDataString(message)}";
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

    public string? CurrentPassword { get; set; }
}

public sealed class ChangePasswordForm
{
    public string? CurrentPassword { get; set; }

    public string? NewPassword { get; set; }

    public string? ConfirmNewPassword { get; set; }
}

public sealed class ForgotPasswordForm
{
    public string? Email { get; set; }
}

public sealed class ResetPasswordForm
{
    public string? UserId { get; set; }

    public string? Token { get; set; }

    public string? Password { get; set; }

    public string? ConfirmPassword { get; set; }
}
