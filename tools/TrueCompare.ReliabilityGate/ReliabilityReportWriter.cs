using System.Net;
using System.Text;

namespace TrueCompare.ReliabilityGate;

public sealed class ReliabilityReportWriter
{
    public async Task WriteAllAsync(ReliabilityReport report, string outputDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        await ReliabilityJson.WriteAsync(Path.Combine(outputDirectory, "reliability-report.json"), report, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "reliability-report.csv"), BuildCsv(report), cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "reliability-report.html"), BuildHtml(report), cancellationToken);
    }

    private static string BuildCsv(ReliabilityReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("case_id,category,subcategory,status,score,critical_failures,warnings,prompt");
        foreach (var testCase in report.Cases)
        {
            builder.AppendLine(string.Join(
                ',',
                Csv(testCase.CaseId),
                Csv(testCase.Category),
                Csv(testCase.Subcategory),
                Csv(testCase.Status.ToString()),
                testCase.Score.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Csv(string.Join(" | ", testCase.CriticalFailures)),
                Csv(string.Join(" | ", testCase.Warnings)),
                Csv(testCase.UserPrompt)));
        }

        return builder.ToString();
    }

    private static string BuildHtml(ReliabilityReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""
        <!doctype html>
        <html lang="pt-PT">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>TrueCompare - Reliability Gate</title>
          <style>
            body{font-family:Arial,sans-serif;margin:0;background:#11151b;color:#edf2f7}
            main{max-width:1180px;margin:0 auto;padding:32px}
            h1,h2,h3{margin:0 0 14px}
            section{background:#1f252e;border:1px solid #333b48;border-radius:10px;margin:18px 0;padding:18px}
            table{width:100%;border-collapse:collapse;margin-top:12px}
            th,td{border-bottom:1px solid #333b48;padding:9px;text-align:left;vertical-align:top}
            th{color:#9aa6b2;font-size:12px;text-transform:uppercase}
            pre{white-space:pre-wrap;background:#151a21;border:1px solid #333b48;border-radius:8px;padding:12px;max-height:360px;overflow:auto}
            .pass{color:#5ee9a8}.fail{color:#ff7b7b}.inc{color:#e9d67b}
            .metric{display:grid;grid-template-columns:repeat(auto-fit,minmax(160px,1fr));gap:10px}
            .metric div{background:#151a21;border:1px solid #333b48;border-radius:8px;padding:12px}
            .badge{display:inline-block;border:1px solid #4b5563;border-radius:999px;padding:3px 8px;margin:2px;color:#cbd5e1}
          </style>
        </head>
        <body><main>
        """);

        var verdictClass = report.Verdict switch
        {
            ReliabilityVerdict.Passou => "pass",
            ReliabilityVerdict.Falhou => "fail",
            _ => "inc"
        };

        builder.AppendLine("<h1>Teste final de fiabilidade TrueCompare</h1>");
        builder.AppendLine("<section>");
        builder.AppendLine("<h2>Veredito final de fiabilidade</h2>");
        builder.AppendLine($"<p class=\"{verdictClass}\"><strong>{Html(report.VerdictText)}</strong></p>");
        builder.AppendLine("</section>");

        builder.AppendLine("<section><h2>Resumo executivo</h2><div class=\"metric\">");
        Metric(builder, "Categorias", report.TotalCategories.ToString());
        Metric(builder, "Subcategorias", report.TotalSubcategories.ToString());
        Metric(builder, "Subcategorias validáveis", report.ValidableSubcategories.ToString());
        Metric(builder, "Casos", report.TotalCases.ToString());
        Metric(builder, "Score agregado", $"{report.AggregateScore:0.##}%");
        Metric(builder, "Falhas críticas", report.CriticalFailureCount.ToString());
        Metric(builder, "Bloqueados", report.BlockedCases.ToString());
        Metric(builder, "Inconclusivos", report.InconclusiveCases.ToString());
        builder.AppendLine("</div></section>");

        if (report.RunWarnings.Count > 0)
        {
            builder.AppendLine("<section><h2>Bloqueios e avisos da execução</h2><ul>");
            foreach (var warning in report.RunWarnings)
            {
                builder.AppendLine($"<li>{Html(warning)}</li>");
            }

            builder.AppendLine("</ul></section>");
        }

        builder.AppendLine("<section><h2>Score por categoria</h2><table><thead><tr><th>Categoria</th><th>Score</th></tr></thead><tbody>");
        foreach (var item in report.ScoreByCategory.OrderBy(item => item.Key))
        {
            builder.AppendLine($"<tr><td>{Html(item.Key)}</td><td>{item.Value:0.##}%</td></tr>");
        }
        builder.AppendLine("</tbody></table></section>");

        if (report.NotValidableSubcategoriesList.Count > 0)
        {
            builder.AppendLine("<section><h2>Subcategorias não validáveis</h2><table><thead><tr><th>Categoria</th><th>Subcategoria</th><th>Motivo</th></tr></thead><tbody>");
            foreach (var subcategory in report.NotValidableSubcategoriesList)
            {
                builder.AppendLine($"<tr><td>{Html(subcategory.Category)}</td><td>{Html(subcategory.Name)}</td><td>{Html(subcategory.NotValidableReason ?? subcategory.Status.ToString())}</td></tr>");
            }
            builder.AppendLine("</tbody></table></section>");
        }

        builder.AppendLine("<section><h2>Casos testados</h2>");
        foreach (var testCase in report.Cases)
        {
            builder.AppendLine("<article>");
            builder.AppendLine($"<h3>{Html(testCase.Category)} / {Html(testCase.Subcategory)} - {testCase.Score}%</h3>");
            builder.AppendLine($"<p><span class=\"badge\">{Html(testCase.Status.ToString())}</span>{(testCase.CriticalFailure ? "<span class=\"badge fail\">falha crítica</span>" : string.Empty)}</p>");
            builder.AppendLine($"<p><strong>Prompt:</strong> {Html(testCase.UserPrompt)}</p>");
            builder.AppendLine("<table><thead><tr><th>Produto app</th><th>Produto KuantoKusta</th><th>Produto ChatGPT</th><th>Preço</th><th>Estado</th></tr></thead><tbody>");
            foreach (var match in testCase.ProductMatches)
            {
                builder.AppendLine($"<tr><td>{Html(match.AppProduct)}</td><td>{Html(match.MatchedMarketProduct ?? "sem match")}</td><td>{Html(match.MatchedChatGptProduct ?? "sem match")}</td><td>{Html(PriceLine(match))}</td><td>{Html(MatchState(match))}</td></tr>");
            }
            builder.AppendLine("</tbody></table>");

            if (testCase.CriticalFailures.Count > 0)
            {
                builder.AppendLine("<h4>Falhas críticas</h4><ul>");
                foreach (var failure in testCase.CriticalFailures)
                {
                    builder.AppendLine($"<li>{Html(failure)}</li>");
                }
                builder.AppendLine("</ul>");
            }

            builder.AppendLine("<details><summary>Evidência ChatGPT</summary>");
            builder.AppendLine($"<p><strong>Modelo:</strong> {Html(testCase.ChatGpt.ModelUsed ?? "não visível")} · <strong>Modo:</strong> {Html(testCase.ChatGpt.ReasoningMode ?? "não visível")}</p>");
            builder.AppendLine("<h4>Prompt enviado</h4><pre>" + Html(testCase.ChatGpt.PromptSent) + "</pre>");
            builder.AppendLine("<h4>Resposta recebida</h4><pre>" + Html(testCase.ChatGpt.ResponseText) + "</pre>");
            builder.AppendLine("</details>");

            builder.AppendLine("<details><summary>Resposta da aplicação</summary><pre>" + Html(testCase.App.ResponseText) + "</pre></details>");
            builder.AppendLine("</article><hr />");
        }
        builder.AppendLine("</section>");

        builder.AppendLine("</main></body></html>");
        return builder.ToString();
    }

    private static void Metric(StringBuilder builder, string label, string value)
    {
        builder.AppendLine($"<div><strong>{Html(value)}</strong><br><span>{Html(label)}</span></div>");
    }

    private static string PriceLine(ProductMatchResult match)
    {
        var app = match.AppPriceCents.HasValue ? ReliabilityNormalizer.FormatCurrency(match.AppPriceCents.Value) : "n/d";
        var market = match.MarketPriceCents.HasValue ? ReliabilityNormalizer.FormatCurrency(match.MarketPriceCents.Value) : "n/d";
        return $"app {app} / mercado {market}";
    }

    private static string MatchState(ProductMatchResult match)
    {
        if (!match.ExistsInMarket) return "produto sem evidência";
        if (!match.PriceCompatible) return "preço desalinhado";
        return "validado";
    }

    private static string Csv(string value)
    {
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
