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
    TargetPriceAlertService alertService) : Controller
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

        var product = comparisonData.FindProduct(input.ProductSlug ?? "macbook-air-m3")
            ?? comparisonData.Products.First();

        if (!TargetPriceAlertService.TryParsePrice(input.TargetPrice, out var targetPrice) || targetPrice <= 0)
        {
            return RedirectWithMessage(input.ReturnUrl, "Define um preço alvo válido.");
        }

        await alertService.CreateAsync(userId, product.Slug, product.Name, targetPrice);

        return RedirectWithMessage(input.ReturnUrl, $"Alerta criado. Enviamos email quando {product.Name} chegar a {targetPrice:0.00} €.");
    }

    private static IActionResult RedirectWithMessage(string? returnUrl, string message)
    {
        var safeReturnUrl = string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/')
            ? "/checkout"
            : returnUrl;

        var separator = safeReturnUrl.Contains('?') ? '&' : '?';
        return new RedirectResult($"{safeReturnUrl}{separator}message={Uri.EscapeDataString(message)}");
    }
}

public sealed class PriceAlertForm
{
    public string? ProductSlug { get; set; }

    public string? TargetPrice { get; set; }

    public string? ReturnUrl { get; set; }
}
