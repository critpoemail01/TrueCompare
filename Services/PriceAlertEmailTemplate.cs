using System.Net;
using TrueCompare.Data;
using TrueCompare.Models;

namespace TrueCompare.Services;

public static class PriceAlertEmailTemplate
{
    public static string BuildSubject(string productName)
    {
        var cleanProductName = productName
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return string.IsNullOrWhiteSpace(cleanProductName)
            ? "TrueCompare - alerta de preço atingido"
            : $"TrueCompare - {cleanProductName} baixou de preço";
    }

    public static string Build(TargetPriceAlert alert, SellerOffer offer, bool isExample = false)
    {
        var productName = Html(alert.ProductName);
        var sellerName = Html(offer.Seller);
        var sellerUrl = Html(string.IsNullOrWhiteSpace(offer.Url) ? "#" : offer.Url);
        var reliability = offer.ReliabilityScore > 0 ? $"{offer.ReliabilityScore}/100" : "Validado";
        var currentPrice = TargetPriceAlertService.FormatCents(offer.PriceCents);
        var targetPrice = TargetPriceAlertService.FormatCents(alert.TargetPriceCents);
        var savingsCents = Math.Max(0, alert.TargetPriceCents - offer.PriceCents);
        var savings = TargetPriceAlertService.FormatCents(savingsCents);
        var eyebrow = isExample ? "Exemplo de alerta" : "Alerta ativo";
        var footerNote = isExample
            ? "Este email &eacute; um exemplo de valida&ccedil;&atilde;o do formato do alerta. Em produ&ccedil;&atilde;o, s&oacute; &eacute; enviado quando existir pre&ccedil;o confirmado em tempo real."
            : "Recebeste este email porque criaste um alerta de pre&ccedil;o no TrueCompare.";

        return $"""
            <!doctype html>
            <html lang="pt">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>TrueCompare - alerta de pre&ccedil;o</title>
            </head>
            <body style="margin:0;padding:0;background:#f3f6f8;color:#111827;font-family:Arial,Helvetica,sans-serif;">
              <div style="display:none;max-height:0;overflow:hidden;color:#f3f6f8;opacity:0;">
                O pre&ccedil;o alvo foi atingido. Confirma pre&ccedil;o, stock e vendedor antes de comprar.
              </div>

              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f3f6f8;padding:38px 14px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:780px;background:#ffffff;border:1px solid #d6f7f5;border-radius:18px;overflow:hidden;box-shadow:0 18px 50px rgba(17,24,39,.12);">
                      <tr>
                        <td style="padding:20px 32px;background:#171a20;border-bottom:1px solid #2d3640;">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                            <tr>
                              <td style="vertical-align:middle;">
                                <div style="color:#7ce7df;font-size:14px;line-height:19px;font-weight:800;letter-spacing:.06em;">&lt;/&gt; TRUECOMPARE</div>
                                <div style="margin-top:3px;color:#a6afba;font-size:13px;line-height:19px;">AI Verified Products</div>
                              </td>
                              <td align="right" style="vertical-align:middle;color:#a6afba;font-size:13px;line-height:19px;">Compra verificada</td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <tr>
                        <td style="padding:38px 42px 32px 42px;background:#20252d;">
                          <div style="display:inline-block;padding:8px 13px;border:1px solid #55d6cf;border-radius:999px;color:#7ce7df;background:#173035;font-size:13px;line-height:17px;font-weight:800;text-transform:uppercase;letter-spacing:.06em;">{eyebrow}</div>
                          <h1 style="margin:20px 0 12px 0;color:#f4f7fb;font-size:40px;line-height:46px;font-weight:900;">O pre&ccedil;o baixou</h1>
                          <p style="margin:0;color:#b6bec8;font-size:18px;line-height:29px;">Encontr&aacute;mos uma oferta igual ou abaixo do pre&ccedil;o alvo que definiste. Confirma as condi&ccedil;&otilde;es na loja antes de pagar.</p>
                        </td>
                      </tr>

                      <tr>
                        <td style="padding:34px 42px 8px 42px;background:#ffffff;">
                          <div style="color:#6b7280;font-size:13px;line-height:19px;font-weight:800;text-transform:uppercase;letter-spacing:.08em;">Produto acompanhado</div>
                          <div style="margin-top:9px;color:#111827;font-size:26px;line-height:34px;font-weight:900;">{productName}</div>
                        </td>
                      </tr>

                      <tr>
                        <td style="padding:22px 42px 24px 42px;background:#ffffff;">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                            <tr>
                              <td width="48%" style="padding:20px 22px;background:#f8fafc;border:1px solid #e5e7eb;border-radius:14px;">
                                <div style="color:#6b7280;font-size:14px;line-height:19px;font-weight:700;">Pre&ccedil;o alvo</div>
                                <div style="margin-top:9px;color:#111827;font-size:30px;line-height:36px;font-weight:900;">{targetPrice}</div>
                              </td>
                              <td width="4%" style="font-size:1px;line-height:1px;">&nbsp;</td>
                              <td width="48%" style="padding:20px 22px;background:#e9fffc;border:1px solid #77e5df;border-radius:14px;">
                                <div style="color:#0f766e;font-size:14px;line-height:19px;font-weight:800;">Pre&ccedil;o encontrado</div>
                                <div style="margin-top:9px;color:#0f766e;font-size:30px;line-height:36px;font-weight:900;">{currentPrice}</div>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <tr>
                        <td style="padding:0 42px 26px 42px;background:#ffffff;">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#171a20;border:1px solid #2d3640;border-radius:16px;">
                            <tr>
                              <td style="padding:24px 26px;">
                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                                  <tr>
                                    <td style="vertical-align:top;">
                                      <div style="color:#a6afba;font-size:13px;line-height:19px;font-weight:800;text-transform:uppercase;letter-spacing:.08em;">Loja recomendada</div>
                                      <div style="margin-top:8px;color:#f4f7fb;font-size:20px;line-height:27px;font-weight:900;">{sellerName}</div>
                                      <div style="margin-top:7px;color:#5ee9a8;font-size:15px;line-height:23px;">Confian&ccedil;a {reliability} - pre&ccedil;o validado</div>
                                    </td>
                                    <td align="right" style="vertical-align:top;">
                                      <div style="color:#a6afba;font-size:13px;line-height:19px;font-weight:800;text-transform:uppercase;letter-spacing:.08em;">Poupan&ccedil;a</div>
                                      <div style="margin-top:8px;color:#f5e66f;font-size:21px;line-height:27px;font-weight:900;">{savings}</div>
                                    </td>
                                  </tr>
                                </table>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <tr>
                        <td style="padding:0 42px 34px 42px;background:#ffffff;">
                          <a href="{sellerUrl}" style="display:block;background:#72ded8;color:#10151b;text-decoration:none;text-align:center;border-radius:999px;padding:19px 24px;font-size:18px;line-height:23px;font-weight:900;">Confirmar pre&ccedil;o na loja &rarr;</a>
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin-top:24px;">
                            <tr>
                              <td style="padding:18px 20px;background:#f8fafc;border:1px solid #e5e7eb;border-radius:14px;color:#4b5563;font-size:15px;line-height:24px;">
                                <strong style="color:#111827;">Antes de comprar:</strong> confirma o pre&ccedil;o final, stock dispon&iacute;vel, vendedor e condi&ccedil;&otilde;es de entrega. A verifica&ccedil;&atilde;o protege-te contra varia&ccedil;&otilde;es de pre&ccedil;o e lojas n&atilde;o confirmadas.
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <tr>
                        <td style="padding:24px 42px;background:#f8fafc;border-top:1px solid #e5e7eb;">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                            <tr>
                              <td style="color:#6b7280;font-size:13px;line-height:20px;">{footerNote}</td>
                              <td align="right" style="color:#0f766e;font-size:13px;line-height:20px;font-weight:800;">TrueCompare by Advance</td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
