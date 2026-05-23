using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TrueCompare.Data;
using TrueCompare.Tests.Support;

namespace TrueCompare.Tests;

public sealed class AppSmokeTests(TrueCompareWebApplicationFactory factory)
    : IClassFixture<TrueCompareWebApplicationFactory>
{
    [Theory]
    [InlineData("/", "O que queres comprar hoje?")]
    [InlineData("/results?query=comprar%20smartphones", "iPhone 16e")]
    [InlineData("/product/iphone-15-pro?query=comprar%20smartphones", "Score IA")]
    [InlineData("/checkout?product=iphone-15-pro", "Sem loja validada")]
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
        Assert.DoesNotContain("Continuar com recomendado", html);
        Assert.DoesNotContain("+ Adicionar produto", html);
        Assert.Contains("Samsung Galaxy S24", html);
        Assert.Contains("iPhone 16e", html);
    }

    [Fact]
    public async Task ResultsPage_ExposesOnlyCardDetailLinks()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/results?query=comprar%20smartphones");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.DoesNotContain("Continuar com recomendado", decoded);
        Assert.DoesNotContain("+ Adicionar produto", decoded);
        Assert.Contains("Ver detalhes", decoded);
        Assert.Contains("/product/", html);
    }

    [Fact]
    public async Task ProductDetail_ExposesCheckoutNextStep()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/product/iphone-17?query=telemovel%20iphone%2017");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("Ver condições", decoded);
        Assert.Contains("/checkout?product=iphone-17", html);
    }

    [Fact]
    public async Task CheckoutPage_RendersProductName()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/checkout?product=iphone-15-pro");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("Produto", decoded);
        Assert.Contains("iPhone 15 Pro", decoded);
    }

    [Fact]
    public async Task CheckoutPage_OnlyShowsConfirmedProductStoreOffers()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/checkout?product=logitech-g305-lightspeed");

        Assert.Contains("https://www.kuantokusta.pt/p/199266/logitech-g305-lightspeed-wireless-gaming-910-005283", html);
        Assert.Contains("target=\"_blank\"", html);
        Assert.Contains("rel=\"noopener noreferrer\"", html);
        Assert.DoesNotContain("/pesquisa/", html);
        Assert.DoesNotContain("/search?", html);
    }

    [Fact]
    public async Task CheckoutPage_RendersConfirmedIphone17StorePriceAndProductPage()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/checkout?product=iphone-17");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("KuantoKusta", decoded);
        Assert.Matches(@"(?<!\d)804,99\s*\u20AC", decoded);
        Assert.Contains("https://www.kuantokusta.pt/p/11928760/apple-iphone-17-63-256gb-black", html);
        Assert.Contains("value=\"724.49\"", html);
        Assert.DoesNotContain("worten.pt/search", html);
        Assert.DoesNotContain("amazon.es/s?k=", html);
        Assert.DoesNotContain("Confirmar na loja", decoded);
    }

    [Fact]
    public async Task ResultsPage_DoesNotRenderProductsWithoutValidatedStoreOffers()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/results?query=comprar%20smartphones");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("iPhone 16e", decoded);
        Assert.Contains("Samsung Galaxy S24", decoded);
        Assert.DoesNotContain("Google Pixel 8", decoded);
        Assert.DoesNotContain("Xiaomi 14", decoded);
    }

    [Fact]
    public async Task ResultsPage_RendersMicrowavesWithValidatedStoreOffers()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/results?query=microondas");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("Teka MW FS20", decoded);
        Assert.Contains("Microondas", decoded);
        Assert.DoesNotContain("Ainda não consegui validar produtos", decoded);
        Assert.DoesNotContain("Bosch Serie 6 Frigorífico", decoded);
    }

    [Fact]
    public async Task ResultsPage_RendersGamingChairInsteadOfConsole_WhenQueryAsksForGamingChair()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/results?query=cadeira%20gaming");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("RACINGREAT Costas Altas Cadeira Gaming", decoded);
        Assert.Contains("65,00", decoded);
        Assert.DoesNotContain("PlayStation 5 Slim", decoded);
        Assert.DoesNotContain("Ainda não consegui validar produtos", decoded);
    }

    [Fact]
    public async Task ResultsPage_RendersDryerInsteadOfFashion_WhenQueryAsksForSecarRoupa()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/results?query=Maquina%20de%20Secar%20Roupa%20Beko%20BM3T48249W%208Kg%20Classe%20C");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("Beko BM3T48249W Maquina de Secar Roupa 8Kg", decoded);
        Assert.Contains("366,90", decoded);
        Assert.DoesNotContain("Adidas", decoded);
        Assert.DoesNotContain("Sapatilhas", decoded);
        Assert.DoesNotContain("Ainda não consegui validar produtos", decoded);
    }

    [Fact]
    public async Task CheckoutPage_RendersConfirmedDryerStorePriceAndProductPage()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/checkout?product=beko-bm3t48249w-maquina-secar-roupa");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("Beko BM3T48249W", decoded);
        Assert.Contains("KuantoKusta", decoded);
        Assert.Contains("366,90", decoded);
        Assert.Contains("value=\"330.21\"", html);
        Assert.Contains("https://www.kuantokusta.pt/p/11598597/beko-bm3t48249w-8kg-classe-c", html);
        Assert.DoesNotContain("/search?", html);
        Assert.DoesNotContain("/pesquisa", html);
    }

    [Fact]
    public async Task CheckoutPage_RendersConfirmedMicrowaveStorePriceAndProductPage()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/checkout?product=teka-mw-fs20-g-wh-microondas");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("Teka MW FS20 G WH", decoded);
        Assert.Contains("Darty", decoded);
        Assert.Contains("64,99", decoded);
        Assert.Contains("value=\"58.49\"", html);
        Assert.Contains("https://darty.pt/products/teka-microond-mw-fs20-g-wh-grill-20", html);
        Assert.DoesNotContain("/search?", html);
        Assert.DoesNotContain("/pesquisa", html);
    }

    [Fact]
    public async Task CheckoutPage_RendersConfirmedGamingChairStorePriceAndProductPage()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/checkout?product=racingreat-costas-altas-cadeira-gaming&query=cadeira%20gaming");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("RACINGREAT Costas Altas Cadeira Gaming", decoded);
        Assert.Contains("Worten", decoded);
        Assert.Contains("65,00", decoded);
        Assert.Contains("value=\"58.50\"", html);
        Assert.Contains("https://www.worten.pt/produtos/cadeira-de-escritorio-ergonomica-racingreat-costas-altas-inclinavel-bracos-regulaveis-preto-mrkean-8711544779636", html);
        Assert.DoesNotContain("Sem loja validada", decoded);
        Assert.DoesNotContain("/search?", html);
        Assert.DoesNotContain("/pesquisa", html);
    }

    [Fact]
    public async Task CheckoutPage_ShowsKuantoKustaSearchFallback_WhenNoConfirmedStoreOfferExists()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/checkout?product=iphone-15-pro");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("Sem loja validada", decoded);
        Assert.Contains("preço confirmado com página direta", decoded);
        Assert.Contains("Pesquisar no KuantoKusta", decoded);
        Assert.Contains("https://www.kuantokusta.pt/search?q=iPhone%2015%20Pro", html);
        Assert.Contains("target=\"_blank\"", html);
        Assert.Contains("rel=\"noopener noreferrer\"", html);
        Assert.DoesNotContain("amazon.es/s?k=", html);
        Assert.DoesNotContain("seller-table", html);
    }

    [Fact]
    public async Task ResultsPage_RendersOnlyIphone17Family_WhenQueryMentionsIphone17Typo()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/results?query=telemovel%20ipohone%2017");

        Assert.Contains("iPhone 17", html);
        Assert.DoesNotContain("iPhone 15 Pro", html);
        Assert.DoesNotContain("Samsung Galaxy S24", html);
        Assert.DoesNotContain("Google Pixel 8", html);
        Assert.DoesNotContain("Xiaomi 14", html);
    }

    [Fact]
    public async Task ResultsPage_RendersMouseCatalog_WhenQueryAsksForMouseUnder50()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/results?query=quero%20um%20rato%20ate%2050%20euros");

        Assert.Contains("Logitech Signature M650", html);
        Assert.DoesNotContain("Microsoft Bluetooth Mouse", html);
        Assert.DoesNotContain("MacBook Air M3", html);
    }

    [Fact]
    public async Task TutorialsPage_RendersPlayableLocalVideoSources()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/tutorials");
        var decoded = WebUtility.HtmlDecode(html);
        var video = await client.GetAsync("/tutorials/primeiro-prompt.webm");

        Assert.Contains("Como criar o primeiro prompt", decoded);
        Assert.Contains("<video", html);
        Assert.Contains("tutorials/primeiro-prompt.webm", html);
        Assert.Contains("tutorials/pagamento-seguro.webm", html);
        Assert.Contains("Reproduzir tutorial", decoded);
        Assert.True(video.IsSuccessStatusCode);
        Assert.Equal("video/webm", video.Content.Headers.ContentType?.MediaType);
        Assert.True(video.Content.Headers.ContentLength > 100_000);
    }

    [Fact]
    public async Task ProductDetail_UsesQueryProductAndRendersOfficialSpecsAndYouTubeReviews()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/product/bosch-serie-6-frigorifico?query=software%20chao%20de%20fabrica%20tablet%20touch%2010%20polegadas%20todoterreno");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("Getac UX10 G3", decoded);
        Assert.DoesNotContain("Bosch Serie 6 Frigor", decoded);
        Assert.Contains("Especificações oficiais", decoded);
        Assert.Contains("Abrir página oficial", decoded);
        Assert.Contains("Reviews no YouTube", decoded);
        Assert.Contains("Versão e lançamento", decoded);
        Assert.Contains("2023", decoded);
        Assert.Contains("UX10/UX10-IP", decoded);
        Assert.Contains("2025", decoded);
        Assert.Contains("youtube.com/results", html);
    }

    [Fact]
    public async Task HomePage_ExposesImageUploadAndPasteSurface()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("id=\"home-prompt-panel\"", html);
        Assert.Contains("id=\"product-image\"", html);
        Assert.Contains("accept=\"image/*\"", html);
        Assert.Contains("type=\"button\"", html);
        Assert.Contains("Adicionar ou colar imagem do produto", html);
        Assert.Contains("placeholder=", html);
        Assert.Contains("Descreve o produto que procuras", decoded);
        Assert.DoesNotContain("value=\"Descreve o produto", decoded);
    }

    [Fact]
    public async Task HomePage_ShowsPortugueseSearchMarketContext()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("A procurar em Portugal", decoded);
        Assert.Contains("Idioma pt-PT", decoded);
        Assert.Contains("moeda EUR", decoded);
        Assert.Contains("LLM", decoded);
        Assert.Contains("desligado", decoded);
        Assert.Contains("Worten", decoded);
        Assert.Contains("Castro Electronica", decoded);
        Assert.Contains("Aquario", decoded);
    }

    [Fact]
    public async Task HomePage_DoesNotPreselectCategory()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("Smartphones premium", decoded);
        Assert.DoesNotContain("category-menu button active", decoded);
        Assert.DoesNotContain("class=\"active\"", decoded);
    }

    [Fact]
    public async Task MainMenu_RendersOnlyRequestedItems()
    {
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/");
        var decoded = WebUtility.HtmlDecode(html);

        Assert.Contains("Novo Chat", decoded);
        Assert.True(decoded.Contains("Alertas ativos") || decoded.Contains("Active alerts"));
        Assert.True(decoded.Contains("Histórico") || decoded.Contains("History"));
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
    [InlineData("/settings")]
    public async Task UserActivityPages_RedirectToLogin_WhenAnonymous(string url)
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("http://localhost/login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task SettingsPage_RendersForAuthenticatedUserAndMenuShowsSettings()
    {
        var signedIn = await CreateSignedInClientAsync();

        var settingsHtml = await GetStringAsync(signedIn, "/settings");
        var decodedSettings = WebUtility.HtmlDecode(settingsHtml);

        Assert.Contains("Definições", decodedSettings);
        Assert.Contains("Dados do utilizador", decodedSettings);
        Assert.Contains($"value=\"{signedIn.Email}\"", decodedSettings);
        Assert.Contains("Alterar password", decodedSettings);

        var homeHtml = await GetStringAsync(signedIn, "/");
        var decodedHome = WebUtility.HtmlDecode(homeHtml);

        Assert.Contains(">Definições<", decodedHome);
        Assert.Contains("Plano", decodedHome);
        Assert.Contains("Local", decodedHome);
        Assert.Contains("Ilimitados", decodedHome);
    }

    [Fact]
    public async Task SettingsProfile_PostUpdatesNameAndEmail()
    {
        var signedIn = await CreateSignedInClientAsync();
        var settingsHtml = await GetStringAsync(signedIn, "/settings");
        var newEmail = $"updated-{Guid.NewGuid():N}@example.com";

        var response = await PostFormAsync(signedIn, "/auth/settings/profile", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ReadAntiforgeryToken(settingsHtml),
            ["DisplayName"] = "Joel Santos",
            ["Email"] = newEmail
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/settings?message=", response.Headers.Location?.ToString());

        var updatedHtml = await GetStringAsync(signedIn, "/settings");
        var decoded = WebUtility.HtmlDecode(updatedHtml);

        Assert.Contains("Joel Santos", decoded);
        Assert.Contains(newEmail, decoded);
    }

    [Fact]
    public async Task SettingsPassword_PostChangesPassword()
    {
        var signedIn = await CreateSignedInClientAsync();
        var settingsHtml = await GetStringAsync(signedIn, "/settings");

        var response = await PostFormAsync(signedIn, "/auth/settings/password", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ReadAntiforgeryToken(settingsHtml),
            ["CurrentPassword"] = signedIn.Password,
            ["NewPassword"] = "NewPassword1!",
            ["ConfirmNewPassword"] = "NewPassword1!"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/settings?message=", response.Headers.Location?.ToString());
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
        Assert.Contains("What do you want to buy today?", html);
        Assert.Contains("Searching in the United States", html);
        Assert.Contains("Language en-US", html);
        Assert.Contains("currency USD", html);
        Assert.Contains("Best Buy, Walmart, Amazon.com", html);
        Assert.Contains("Create account", html);
        Assert.DoesNotContain("O que queres comprar hoje?", html);
    }

    [Fact]
    public async Task GoogleLogin_RedirectsToLogin_WhenOAuthIsNotConfigured()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var response = await client.GetAsync("/auth/google?returnUrl=/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/login?message=", response.Headers.Location?.ToString());
    }

    private async Task<SignedInTestClient> CreateSignedInClientAsync()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var email = $"settings-{Guid.NewGuid():N}@example.com";
        const string password = "TestPassword1!";
        var signedIn = new SignedInTestClient(client, new Dictionary<string, string>(StringComparer.Ordinal), email, password);
        var registerHtml = await GetStringAsync(signedIn, "/register?returnUrl=/settings");
        var response = await PostFormAsync(signedIn, "/auth/register", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ReadAntiforgeryToken(registerHtml),
            ["Email"] = email,
            ["Password"] = password,
            ["ConfirmPassword"] = password,
            ["ReturnUrl"] = "/settings"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/settings", response.Headers.Location?.ToString());

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var persistedUser = await userManager.FindByEmailAsync(email);
            Assert.NotNull(persistedUser);
            Assert.True(await userManager.CheckPasswordAsync(persistedUser, password));
        }

        var loginHtml = await GetStringAsync(signedIn, "/login?returnUrl=/settings");
        response = await PostFormAsync(signedIn, "/auth/login", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ReadAntiforgeryToken(loginHtml),
            ["Email"] = email,
            ["Password"] = password,
            ["ReturnUrl"] = "/settings"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/settings", response.Headers.Location?.ToString());
        Assert.Contains(".AspNetCore.Identity.Application", signedIn.Cookies.Keys);

        return signedIn;
    }

    private static async Task<string> GetStringAsync(SignedInTestClient signedIn, string url)
    {
        using var response = await SendAsync(signedIn, new HttpRequestMessage(HttpMethod.Get, url));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static Task<HttpResponseMessage> PostFormAsync(
        SignedInTestClient signedIn,
        string url,
        Dictionary<string, string> form)
    {
        return SendAsync(signedIn, new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(form)
        });
    }

    private static async Task<HttpResponseMessage> SendAsync(SignedInTestClient signedIn, HttpRequestMessage request)
    {
        ApplyCookies(signedIn, request);
        var response = await signedIn.Client.SendAsync(request);
        CaptureCookies(signedIn, response);
        return response;
    }

    private static void ApplyCookies(SignedInTestClient signedIn, HttpRequestMessage request)
    {
        request.Headers.Remove("Cookie");
        if (signedIn.Cookies.Count > 0)
        {
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                string.Join("; ", signedIn.Cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));
        }
    }

    private static void CaptureCookies(SignedInTestClient signedIn, HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            return;
        }

        foreach (var setCookie in setCookies)
        {
            var cookiePair = setCookie.Split(';', 2)[0];
            var separator = cookiePair.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            signedIn.Cookies[cookiePair[..separator]] = cookiePair[(separator + 1)..];
        }
    }

    private static string ReadAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        Assert.True(match.Success, "Expected an antiforgery token in the rendered form.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private sealed record SignedInTestClient(
        HttpClient Client,
        Dictionary<string, string> Cookies,
        string Email,
        string Password);
}


