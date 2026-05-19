using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using TrueCompare.Data;
using TrueCompare.Options;

namespace TrueCompare.Controllers;

public sealed class BillingController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IOptions<StripeOptions> stripeOptionsAccessor) : Controller
{
    private readonly StripeOptions stripeOptions = stripeOptionsAccessor.Value;

    [HttpPost("/billing/create-checkout-session")]
    [Authorize]
    [EnableRateLimiting("billing")]
    public async Task<IActionResult> CreateCheckoutSession([FromForm] string packageId)
    {
        var package = stripeOptions.CreditPackages.FirstOrDefault(candidate => candidate.Id == packageId);
        if (package is null)
        {
            return Redirect("/credits?message=Pacote de créditos inválido.");
        }

        if (string.IsNullOrWhiteSpace(stripeOptions.SecretKey))
        {
            return Redirect("/credits?message=Stripe ainda não configurado. Define Stripe:SecretKey.");
        }

        var userId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Redirect("/login?returnUrl=/credits");
        }

        var origin = $"{Request.Scheme}://{Request.Host}";
        var options = new SessionCreateOptions
        {
            ClientReferenceId = userId,
            Mode = "payment",
            PaymentMethodTypes = new List<string> { "card" },
            SuccessUrl = $"{origin}/credits/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{origin}/credits?message=Pagamento cancelado.",
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId,
                ["packageId"] = package.Id,
                ["credits"] = package.Credits.ToString()
            },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = package.Currency,
                        UnitAmount = package.AmountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = package.Name,
                            Description = $"{package.Credits} pesquisas adicionais TrueCompare"
                        }
                    }
                }
            }
        };

        var service = new SessionService(new StripeClient(stripeOptions.SecretKey));
        var session = await service.CreateAsync(options);

        dbContext.CreditPurchases.Add(new CreditPurchase
        {
            UserId = userId,
            StripeSessionId = session.Id,
            Credits = package.Credits,
            AmountCents = package.AmountCents,
            Currency = package.Currency,
            Status = PurchaseStatus.Pending
        });
        await dbContext.SaveChangesAsync();

        return Redirect(session.Url);
    }

    [HttpPost("/billing/stripe-webhook")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> StripeWebhook()
    {
        if (string.IsNullOrWhiteSpace(stripeOptions.WebhookSecret))
        {
            return BadRequest("Stripe webhook secret is not configured.");
        }

        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, stripeOptions.WebhookSecret);
        }
        catch (StripeException)
        {
            return BadRequest();
        }

        if (stripeEvent.Type == "checkout.session.completed"
            && stripeEvent.Data.Object is Session session
            && string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            await CompletePurchaseAsync(session);
        }

        return Ok();
    }

    private async Task CompletePurchaseAsync(Session session)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var purchase = await dbContext.CreditPurchases
            .SingleOrDefaultAsync(candidate => candidate.StripeSessionId == session.Id);

        if (purchase is null || purchase.Status == PurchaseStatus.Paid)
        {
            return;
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == purchase.UserId);
        if (user is null)
        {
            purchase.Status = PurchaseStatus.Failed;
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return;
        }

        user.Credits += purchase.Credits;
        purchase.Status = PurchaseStatus.Paid;
        purchase.StripePaymentIntentId = session.PaymentIntentId;
        purchase.CompletedUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
