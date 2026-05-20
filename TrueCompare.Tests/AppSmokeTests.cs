using System.Net;
using TrueCompare.Tests.Support;

namespace TrueCompare.Tests;

public sealed class AppSmokeTests(TrueCompareWebApplicationFactory factory)
    : IClassFixture<TrueCompareWebApplicationFactory>
{
    [Theory]
    [InlineData("/", "O que queres comparar hoje?")]
    [InlineData("/criteria?query=comprar%20smartphones", "Define o que valorizas")]
    [InlineData("/results?query=comprar%20smartphones", "iPhone 15 Pro")]
    [InlineData("/product/iphone-15-pro?query=comprar%20smartphones", "Score IA")]
    [InlineData("/checkout?product=iphone-15-pro", "Encomendar agora")]
    [InlineData("/login", "Entrar com Gmail")]
    [InlineData("/register", "Criar com Gmail")]
    public async Task PublicPages_RenderExpectedContent(string url, string expectedContent)
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync(url);

        Assert.Contains(expectedContent, html);
        Assert.DoesNotContain("Status Code: 500", html);
    }

    [Fact]
    public async Task ResultsPage_RendersAiSuggestionPanelAndSmartphoneCatalog()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/results?query=comprar%20smartphones");

        Assert.Contains("Sugest", html);
        Assert.Contains("Modo local", html);
        Assert.Contains("Samsung Galaxy S24", html);
        Assert.Contains("Xiaomi 14", html);
    }

    [Fact]
    public async Task ResultsPage_RendersMouseCatalog_WhenQueryAsksForMouseUnder50()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/results?query=quero%20um%20rato%20ate%2050%20euros");

        Assert.Contains("Logitech M650 Signature", html);
        Assert.Contains("Microsoft Bluetooth Mouse", html);
        Assert.DoesNotContain("MacBook Air M3", html);
    }

    [Fact]
    public async Task CriteriaPage_RendersProductSpecificWeights()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/criteria?query=quero%20um%20rato%20ate%2050%20euros");

        Assert.Contains("Sensor / DPI", html);
        Assert.Contains("Ergonomia", html);
        Assert.DoesNotContain("Performance (CPU)", html);
    }

    [Fact]
    public async Task HomePage_ExposesImageUploadAndPasteSurface()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("id=\"home-prompt-panel\"", html);
        Assert.Contains("id=\"product-image\"", html);
        Assert.Contains("accept=\"image/*\"", html);
        Assert.Contains("Adicionar ou colar imagem do produto", html);
    }

    [Fact]
    public async Task MainMenu_RendersOnlyRequestedItems()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("Novo Chat", decoded);
        Assert.True(decoded.Contains("Alertas ativos") || decoded.Contains("Active alerts"));
        Assert.True(decoded.Contains("Histórico de produtos pesquisados") || decoded.Contains("Searched product history"));
        Assert.DoesNotContain(">Categorias<", decoded);
        Assert.DoesNotContain(">Categories<", decoded);
        Assert.DoesNotContain(">Atividade<", decoded);
        Assert.DoesNotContain(">Activity<", decoded);
        Assert.DoesNotContain(">Definições<", decoded);
        Assert.DoesNotContain(">Settings<", decoded);
    }

    [Theory]
    [InlineData("/alerts")]
    [InlineData("/history")]
    public async Task UserActivityPages_RedirectToLogin_WhenAnonymous(string url)
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("http://localhost/login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task HomePage_UsesBrowserLanguage_WhenAcceptLanguageIsEnglish()
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

        using var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("en-US", response.Content.Headers.ContentLanguage);
        Assert.Contains("What do you want to compare today?", html);
        Assert.Contains("Create account", html);
        Assert.DoesNotContain("O que queres comparar hoje?", html);
    }

    [Fact]
    public async Task GoogleLogin_RedirectsToLogin_WhenOAuthIsNotConfigured()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/auth/google?returnUrl=/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/login?message=", response.Headers.Location?.ToString());
    }
}
