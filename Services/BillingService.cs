using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using TrueCompare.Data;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed class BillingService(
    ApplicationDbContext dbContext,
    IOptions<StripeOptions> stripeOptionsAccessor,
    AppText text)
{
    private readonly StripeOptions stripeOptions = stripeOptionsAccessor.Value;

    public async Task<BillingRedirectResult> CreateCreditCheckoutSessionAsync(
        string userId,
        string origin,
        string packageId)
    {
        var package = stripeOptions.CreditPackages.FirstOrDefault(candidate => candidate.Id == packageId);
        if (package is null)
        {
            return BillingRedirectResult.Failure(CreditsRedirect(text.Pick("Pacote de creditos invalido.", "Invalid credit package.")));
        }

        if (string.IsNullOrWhiteSpace(stripeOptions.SecretKey))
        {
            return BillingRedirectResult.Failure(CreditsRedirect(text.Pick("Stripe ainda nao configurado. Define Stripe:SecretKey.", "Stripe is not configured yet. Set Stripe:SecretKey.")));
        }

        var options = new SessionCreateOptions
        {
            ClientReferenceId = userId,
            Mode = "payment",
            PaymentMethodTypes = new List<string> { "card" },
            SuccessUrl = $"{origin}/credits/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{origin}/credits?message={Uri.EscapeDataString(text.Pick("Pagamento cancelado.", "Payment cancelled."))}",
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
                            Description = text.Pick($"{package.Credits} pesquisas adicionais TrueCompare", $"{package.Credits} additional TrueCompare searches")
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

        return BillingRedirectResult.Success(session.Url);
    }

    public async Task<BillingRedirectResult> CreateSubscriptionCheckoutSessionAsync(
        string userId,
        string origin,
        string planId)
    {
        var plan = stripeOptions.SubscriptionPlans.FirstOrDefault(candidate => candidate.Id == planId);
        if (plan is null)
        {
            return BillingRedirectResult.Failure(CreditsRedirect(text["Credits.InvalidSubscription"]));
        }

        if (string.IsNullOrWhiteSpace(stripeOptions.SecretKey))
        {
            return BillingRedirectResult.Failure(CreditsRedirect(text.Pick("Stripe ainda nao configurado. Define Stripe:SecretKey.", "Stripe is not configured yet. Set Stripe:SecretKey.")));
        }

        var metadata = BuildSubscriptionMetadata(userId, plan);
        var options = new SessionCreateOptions
        {
            ClientReferenceId = userId,
            Mode = "subscription",
            PaymentMethodTypes = new List<string> { "card" },
            SuccessUrl = $"{origin}/credits/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{origin}/credits?message={Uri.EscapeDataString(text.Pick("Pagamento cancelado.", "Payment cancelled."))}",
            Metadata = metadata,
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = metadata
            },
            LineItems = new List<SessionLineItemOptions>
            {
                BuildSubscriptionLineItem(plan)
            }
        };

        var service = new SessionService(new StripeClient(stripeOptions.SecretKey));
        var session = await service.CreateAsync(options);

        dbContext.CreditPurchases.Add(new CreditPurchase
        {
            UserId = userId,
            StripeSessionId = session.Id,
            StripeSubscriptionId = session.SubscriptionId,
            BillingKind = CreditBillingKind.Subscription,
            PlanId = plan.Id,
            BillingPeriod = NormalizeStripeInterval(plan.BillingPeriod),
            Credits = plan.CreditsPerPeriod,
            AmountCents = plan.AmountCents,
            Currency = plan.Currency,
            Status = PurchaseStatus.Pending
        });
        await dbContext.SaveChangesAsync();

        return BillingRedirectResult.Success(session.Url);
    }

    public async Task<bool> HandleStripeWebhookAsync(string json, string? signature)
    {
        if (string.IsNullOrWhiteSpace(stripeOptions.WebhookSecret))
        {
            return false;
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, stripeOptions.WebhookSecret);
        }
        catch (StripeException)
        {
            return false;
        }

        if (stripeEvent.Type == "checkout.session.completed"
            && stripeEvent.Data.Object is Session session
            && string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            await CompletePurchaseAsync(session);
        }
        else if (stripeEvent.Type == "invoice.paid" && stripeEvent.Data.Object is Invoice invoice)
        {
            await CompleteSubscriptionRenewalAsync(invoice);
        }
        else if (stripeEvent.Type == "customer.subscription.deleted" && stripeEvent.Data.Object is Subscription subscription)
        {
            await DeactivateSubscriptionAsync(subscription);
        }

        return true;
    }

    private static string CreditsRedirect(string message)
    {
        return $"/credits?message={Uri.EscapeDataString(message)}";
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

        if (string.Equals(purchase.BillingKind, CreditBillingKind.Subscription, StringComparison.OrdinalIgnoreCase))
        {
            ActivateUnlimitedSubscription(user, purchase.PlanId, purchase.BillingPeriod, session.SubscriptionId ?? purchase.StripeSubscriptionId);
            purchase.Credits = 0;
        }
        else
        {
            user.Credits += purchase.Credits;
        }

        purchase.Status = PurchaseStatus.Paid;
        purchase.StripePaymentIntentId = session.PaymentIntentId;
        purchase.StripeSubscriptionId ??= session.SubscriptionId;
        purchase.CompletedUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private async Task CompleteSubscriptionRenewalAsync(Invoice invoice)
    {
        if (!string.Equals(invoice.BillingReason, "subscription_cycle", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var metadata = invoice.Parent?.SubscriptionDetails?.Metadata ?? invoice.Metadata;
        if (!TryGetMetadata(metadata, "userId", out var userId)
            || !TryGetMetadata(metadata, "planId", out var planId))
        {
            return;
        }

        var plan = stripeOptions.SubscriptionPlans.FirstOrDefault(candidate => candidate.Id == planId);
        if (plan is null)
        {
            return;
        }

        var invoicePurchaseId = $"invoice:{invoice.Id}";
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var alreadyCredited = await dbContext.CreditPurchases
            .AnyAsync(candidate => candidate.StripeSessionId == invoicePurchaseId);
        if (alreadyCredited)
        {
            return;
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId);
        if (user is null)
        {
            return;
        }

        ActivateUnlimitedSubscription(
            user,
            plan.Id,
            plan.BillingPeriod,
            invoice.Parent?.SubscriptionDetails?.SubscriptionId);
        dbContext.CreditPurchases.Add(new CreditPurchase
        {
            UserId = userId,
            StripeSessionId = invoicePurchaseId,
            StripeSubscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId,
            BillingKind = CreditBillingKind.Subscription,
            PlanId = planId,
            BillingPeriod = plan.BillingPeriod,
            Credits = 0,
            AmountCents = invoice.AmountPaid,
            Currency = invoice.Currency,
            Status = PurchaseStatus.Paid,
            CompletedUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static Dictionary<string, string> BuildSubscriptionMetadata(string userId, SubscriptionPlan plan)
    {
        return new Dictionary<string, string>
        {
            ["userId"] = userId,
            ["planId"] = plan.Id,
            ["credits"] = plan.CreditsPerPeriod.ToString(),
            ["unlimitedCredits"] = (plan.CreditsPerPeriod <= 0).ToString(),
            ["billingKind"] = CreditBillingKind.Subscription,
            ["billingPeriod"] = NormalizeStripeInterval(plan.BillingPeriod)
        };
    }

    private static SessionLineItemOptions BuildSubscriptionLineItem(SubscriptionPlan plan)
    {
        if (!string.IsNullOrWhiteSpace(plan.StripePriceId))
        {
            return new SessionLineItemOptions
            {
                Price = plan.StripePriceId,
                Quantity = 1
            };
        }

        return new SessionLineItemOptions
        {
            Quantity = 1,
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = plan.Currency,
                UnitAmount = plan.AmountCents,
                Recurring = new SessionLineItemPriceDataRecurringOptions
                {
                    Interval = NormalizeStripeInterval(plan.BillingPeriod)
                },
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = plan.Name,
                    Description = plan.Description
                }
            }
        };
    }

    private static string NormalizeStripeInterval(string? billingPeriod)
    {
        return string.Equals(billingPeriod, "year", StringComparison.OrdinalIgnoreCase) ? "year" : "month";
    }

    private static void ActivateUnlimitedSubscription(
        ApplicationUser user,
        string? planId,
        string? billingPeriod,
        string? stripeSubscriptionId)
    {
        user.HasUnlimitedSubscription = true;
        user.SubscriptionPlanId = planId;
        user.StripeSubscriptionId = stripeSubscriptionId;
        user.SubscriptionActiveUntilUtc = CalculateSubscriptionActiveUntilUtc(billingPeriod, DateTime.UtcNow);
    }

    private static DateTime CalculateSubscriptionActiveUntilUtc(string? billingPeriod, DateTime utcNow)
    {
        return string.Equals(NormalizeStripeInterval(billingPeriod), "year", StringComparison.OrdinalIgnoreCase)
            ? utcNow.AddYears(1)
            : utcNow.AddMonths(1);
    }

    private async Task DeactivateSubscriptionAsync(Subscription subscription)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.StripeSubscriptionId == subscription.Id);

        if (user is null
            && TryGetMetadata(subscription.Metadata, "userId", out var userId))
        {
            user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId);
        }

        if (user is null)
        {
            return;
        }

        user.HasUnlimitedSubscription = false;
        user.SubscriptionActiveUntilUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    private static bool TryGetMetadata(IReadOnlyDictionary<string, string>? metadata, string key, out string value)
    {
        value = string.Empty;
        return metadata is not null
            && metadata.TryGetValue(key, out var found)
            && !string.IsNullOrWhiteSpace(value = found);
    }
}

public sealed record BillingRedirectResult(
    bool IsSuccess,
    string RedirectUrl)
{
    public static BillingRedirectResult Success(string redirectUrl)
    {
        return new BillingRedirectResult(true, redirectUrl);
    }

    public static BillingRedirectResult Failure(string redirectUrl)
    {
        return new BillingRedirectResult(false, redirectUrl);
    }
}
