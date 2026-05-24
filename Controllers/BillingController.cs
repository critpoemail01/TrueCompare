using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using TrueCompare.Data;
using TrueCompare.Options;
using TrueCompare.Services;

namespace TrueCompare.Controllers;

public sealed class BillingController(
    UserManager<ApplicationUser> userManager,
    BillingService billingService,
    IOptions<AppOptions> appOptionsAccessor,
    IWebHostEnvironment environment,
    AppText text) : Controller
{
    private const long MaxStripeWebhookBytes = 1024 * 1024;

    private readonly AppOptions appOptions = appOptionsAccessor.Value;

    [HttpPost("/billing/create-checkout-session")]
    [Authorize]
    [EnableRateLimiting("billing")]
    public async Task<IActionResult> CreateCheckoutSession([FromForm] string packageId)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Redirect("/login?returnUrl=/credits");
        }

        if (!TryGetPublicOrigin(out var origin))
        {
            return Redirect(CreditsRedirect(text.Pick(
                "Define App:PublicBaseUrl antes de iniciar pagamentos em produção.",
                "Set App:PublicBaseUrl before starting production payments.")));
        }

        var result = await billingService.CreateCreditCheckoutSessionAsync(userId, origin, packageId);
        return Redirect(result.RedirectUrl);
    }

    [HttpPost("/billing/create-subscription-session")]
    [Authorize]
    [EnableRateLimiting("billing")]
    public async Task<IActionResult> CreateSubscriptionSession([FromForm] string planId)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Redirect("/login?returnUrl=/credits");
        }

        if (!TryGetPublicOrigin(out var origin))
        {
            return Redirect(CreditsRedirect(text.Pick(
                "Define App:PublicBaseUrl antes de iniciar pagamentos em produção.",
                "Set App:PublicBaseUrl before starting production payments.")));
        }

        var result = await billingService.CreateSubscriptionCheckoutSessionAsync(userId, origin, planId);
        return Redirect(result.RedirectUrl);
    }

    [HttpPost("/billing/stripe-webhook")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> StripeWebhook()
    {
        if (Request.ContentLength is > MaxStripeWebhookBytes)
        {
            return BadRequest();
        }

        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        if (json.Length > MaxStripeWebhookBytes)
        {
            return BadRequest();
        }

        var handled = await billingService.HandleStripeWebhookAsync(json, Request.Headers["Stripe-Signature"].ToString());
        return handled ? Ok() : BadRequest();
    }

    private bool TryGetPublicOrigin(out string origin)
    {
        origin = string.Empty;
        var configuredBaseUrl = appOptions.PublicBaseUrl?.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl)
            && Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var configuredUri)
            && IsAllowedPublicScheme(configuredUri))
        {
            origin = configuredUri.GetLeftPart(UriPartial.Authority);
            return true;
        }

        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            origin = $"{Request.Scheme}://{Request.Host}";
            return true;
        }

        return false;
    }

    private bool IsAllowedPublicScheme(Uri uri)
    {
        return uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || (environment.IsDevelopment() && uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase));
    }

    private static string CreditsRedirect(string message)
    {
        return $"/credits?message={Uri.EscapeDataString(message)}";
    }
}
