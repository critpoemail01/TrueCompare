using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrueCompare.Data;
using TrueCompare.Services;

namespace TrueCompare.Controllers;

public sealed class BillingController(
    UserManager<ApplicationUser> userManager,
    BillingService billingService) : Controller
{
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

        var result = await billingService.CreateCreditCheckoutSessionAsync(userId, RequestOrigin(), packageId);
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

        var result = await billingService.CreateSubscriptionCheckoutSessionAsync(userId, RequestOrigin(), planId);
        return Redirect(result.RedirectUrl);
    }

    [HttpPost("/billing/stripe-webhook")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var handled = await billingService.HandleStripeWebhookAsync(json, Request.Headers["Stripe-Signature"].ToString());
        return handled ? Ok() : BadRequest();
    }

    private string RequestOrigin()
    {
        return $"{Request.Scheme}://{Request.Host}";
    }
}
