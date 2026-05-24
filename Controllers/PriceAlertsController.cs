using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrueCompare.Data;
using TrueCompare.Services;

namespace TrueCompare.Controllers;

[Authorize]
public sealed class PriceAlertsController(
    UserManager<ApplicationUser> userManager,
    ComparisonDataService comparisonData,
    TargetPriceAlertService alertService,
    AppText text) : Controller
{
    [HttpPost("/price-alerts/create")]
    [EnableRateLimiting("alerts")]
    public async Task<IActionResult> Create([FromForm] PriceAlertForm input)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Redirect($"/login?returnUrl={Uri.EscapeDataString(input.ReturnUrl ?? "/checkout")}");
        }

        if (string.IsNullOrWhiteSpace(input.ProductSlug))
        {
            return RedirectWithMessage(
                input.ReturnUrl,
                text.Pick("Escolhe um produto valido para criar o alerta.", "Choose a valid product to create the alert."));
        }

        var product = comparisonData.FindProduct(input.ProductSlug);
        if (product is null)
        {
            return RedirectWithMessage(
                input.ReturnUrl,
                text.Pick("Este produto nao existe no catalogo de alertas.", "This product is not available in the alert catalog."));
        }

        if (!TargetPriceAlertService.TryParsePrice(input.TargetPrice, out var targetPrice) || targetPrice <= 0)
        {
            return RedirectWithMessage(input.ReturnUrl, text.Pick("Define um preço alvo válido.", "Set a valid target price."));
        }

        CreatedPriceAlert createdAlert;
        try
        {
            createdAlert = await alertService.CreateAsync(userId, product.Slug, product.Name, targetPrice);
        }
        catch (PriceAlertCreationException exception)
        {
            var failureMessage = exception.Code switch
            {
                PriceAlertCreationError.ActiveAlertLimitReached => text.Pick(
                    "Atingiste o limite de alertas ativos para a tua conta.",
                    "You reached the active alert limit for your account."),
                _ => text.Pick(
                    "Só é possível criar alerta quando existe uma loja confirmada para este produto.",
                    "A price alert can only be created when this product has a confirmed store.")
            };

            return RedirectWithMessage(input.ReturnUrl, failureMessage);
        }

        var message = createdAlert.IsLiveValidated
            ? text.Pick(
                $"Alerta criado com preço validado. Enviamos email quando {product.Name} chegar a {targetPrice:0.00} €.",
                $"Alert created with a validated price. We will email you when {product.Name} reaches {targetPrice:0.00} €.")
            : text.Pick(
                $"Alerta criado. Vamos revalidar a loja antes de enviar qualquer email sobre {product.Name}.",
                $"Alert created. We will revalidate the store before sending any email about {product.Name}.");

        return RedirectWithMessage(input.ReturnUrl, message);
    }

    private static IActionResult RedirectWithMessage(string? returnUrl, string message)
    {
        var safeReturnUrl = IsSafeLocalPath(returnUrl)
            ? returnUrl!
            : "/checkout";

        var separator = safeReturnUrl.Contains('?') ? '&' : '?';
        return new RedirectResult($"{safeReturnUrl}{separator}message={Uri.EscapeDataString(message)}");
    }

    private static bool IsSafeLocalPath(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl)
            && returnUrl.StartsWith("/", StringComparison.Ordinal)
            && !returnUrl.StartsWith("//", StringComparison.Ordinal)
            && !returnUrl.StartsWith("/\\", StringComparison.Ordinal);
    }
}

public sealed class PriceAlertForm
{
    public string? ProductSlug { get; set; }

    public string? TargetPrice { get; set; }

    public string? ReturnUrl { get; set; }
}
