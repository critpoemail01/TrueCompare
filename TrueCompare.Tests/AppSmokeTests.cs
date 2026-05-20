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
