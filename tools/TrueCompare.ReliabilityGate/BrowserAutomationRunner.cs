using System.Diagnostics;
using System.Text.Json;

namespace TrueCompare.ReliabilityGate;

public sealed class BrowserAutomationRunner
{
    private readonly string projectDirectory;

    public BrowserAutomationRunner(string projectDirectory)
    {
        this.projectDirectory = projectDirectory;
    }

    public async Task<MarketCatalog?> TryCollectKuantoKustaAsync(
        ReliabilityRunOptions options,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        var outputPath = Path.GetFullPath(Path.Combine(outputDirectory, "kuantokusta-live-catalog.json"));
        var arguments = new List<string>
        {
            Path.Combine(projectDirectory, "Browser", "kuantokusta-live.mjs"),
            "--output",
            outputPath,
            "--max-products",
            options.MaxProductsPerSubcategory.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(options.CategoryFilter))
        {
            arguments.Add("--category");
            arguments.Add(options.CategoryFilter!);
        }

        await RunNodeAsync(arguments, Path.GetFullPath(outputDirectory), "kuantokusta-live.log", cancellationToken);
        return File.Exists(outputPath)
            ? await ReliabilityJson.ReadAsync<MarketCatalog>(outputPath, cancellationToken)
            : null;
    }

    public async Task<ChatGptReference> AskChatGptBrowserAsync(
        ReliabilityRunOptions options,
        ReliabilityCase testCase,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        var caseDirectory = Path.GetFullPath(Path.Combine(outputDirectory, "chatgpt", testCase.Id));
        Directory.CreateDirectory(caseDirectory);
        var promptPath = Path.Combine(caseDirectory, "prompt.txt");
        var outputPath = Path.Combine(caseDirectory, "response.json");
        var prompt = ReliabilityPromptGenerator.BuildChatGptPrompt(testCase);
        await File.WriteAllTextAsync(promptPath, prompt, cancellationToken);

        var arguments = new List<string>
        {
            Path.Combine(projectDirectory, "Browser", "chatgpt-browser-reference.mjs"),
            "--prompt-file",
            promptPath,
            "--output",
            outputPath,
            "--screenshot",
            Path.GetFullPath(Path.Combine(caseDirectory, "chatgpt.png")),
            "--model",
            options.ChatGptModelPreference,
            "--reasoning",
            options.ChatGptReasoningMode
        };

        if (!string.IsNullOrWhiteSpace(options.ChromeCdpEndpoint))
        {
            arguments.Add("--cdp");
            arguments.Add(options.ChromeCdpEndpoint!);
        }

        if (!string.IsNullOrWhiteSpace(options.ChromeUserDataDir))
        {
            arguments.Add("--user-data-dir");
            arguments.Add(options.ChromeUserDataDir!);
        }

        try
        {
            await RunNodeAsync(arguments, caseDirectory, "chatgpt-browser.log", cancellationToken);
            if (File.Exists(outputPath))
            {
                var reference = await ReliabilityJson.ReadAsync<ChatGptReference>(outputPath, cancellationToken);
                if (reference is not null)
                {
                    return reference;
                }
            }
        }
        catch (Exception ex)
        {
            return Blocked(prompt, $"Erro ao automatizar ChatGPT no browser: {ex.Message}");
        }

        return Blocked(prompt, "Automação ChatGPT no browser não produziu resposta auditável.");
    }

    private async Task RunNodeAsync(
        IReadOnlyList<string> arguments,
        string workingDirectory,
        string logFileName,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "node",
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Não foi possível iniciar o processo node.");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;
        await File.WriteAllTextAsync(Path.Combine(workingDirectory, logFileName), output + Environment.NewLine + error, cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Node saiu com código {process.ExitCode}. Ver log {logFileName}.");
        }
    }

    private static ChatGptReference Blocked(string prompt, string reason)
    {
        return new ChatGptReference(
            EvidenceStatus.Blocked,
            prompt,
            string.Empty,
            null,
            null,
            DateTimeOffset.UtcNow,
            null,
            reason,
            Array.Empty<string>());
    }
}
