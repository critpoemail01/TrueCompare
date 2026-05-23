using Microsoft.AspNetCore.Mvc;
using TrueCompare.Models;
using TrueCompare.Services;

namespace TrueCompare.Controllers;

public sealed class DevEmailController(
    IWebHostEnvironment environment,
    IEmailSender emailSender) : Controller
{
    [HttpGet("/dev/email/price-alert-preview")]
    public IActionResult PreviewPriceAlert()
    {
        if (!CanUseDevEmailEndpoint())
        {
            return NotFound();
        }

        return Content(BuildExampleEmail(), "text/html; charset=utf-8");
    }

    [HttpPost("/dev/email/send-price-alert-test")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SendPriceAlertTest([FromForm] string? to, CancellationToken cancellationToken)
    {
        if (!CanUseDevEmailEndpoint())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            return BadRequest(new { sent = false, error = "Email de destino obrigatorio." });
        }

        var sent = await emailSender.SendAsync(
            to.Trim(),
            PriceAlertEmailTemplate.BuildSubject(ExampleProductName),
            BuildExampleEmail(),
            cancellationToken);

        return sent
            ? Ok(new { sent = true })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                sent = false,
                error = "SMTP nao configurado. Define Email:Host e Email:FromEmail para enviar emails reais pela aplicacao."
            });
    }

    private bool CanUseDevEmailEndpoint()
    {
        return environment.IsDevelopment()
            && HttpContext.Connection.RemoteIpAddress is { } remoteIpAddress
            && IPAddressIsLocal(remoteIpAddress);
    }

    private static bool IPAddressIsLocal(System.Net.IPAddress address)
    {
        return System.Net.IPAddress.IsLoopback(address)
            || address.MapToIPv4().ToString() == "127.0.0.1";
    }

    private static string BuildExampleEmail()
    {
        var alert = new PriceAlertEmailModel(ExampleProductName, 5500);
        var offer = new SellerOffer(
            "Worten",
            "52,90 €",
            5290,
            "1-2 dias",
            "3 anos PT",
            "Verificado",
            "http://localhost:5190/checkout?product=western-digital-my-passport",
            true,
            93,
            "Portugal",
            "Preço validado",
            true);

        return PriceAlertEmailTemplate.Build(alert, offer, isExample: true);
    }

    private const string ExampleProductName = "Western Digital My Passport 1TB";
}
