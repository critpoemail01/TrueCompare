using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrueCompare.Components;
using TrueCompare.Data;
using TrueCompare.Options;
using TrueCompare.Services;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");
if (isDevelopment)
{
    builder.WebHost.UseStaticWebAssets();
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? string.Empty;
if (string.IsNullOrWhiteSpace(connectionString))
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        connectionString = "Server=(localdb)\\mssqllocaldb;Database=TrueCompareTests;Trusted_Connection=True;TrustServerCertificate=True";
    }
    else
    {
        throw new InvalidOperationException("Connection string 'DefaultConnection' is empty. Configure it through environment variables, User Secrets or a secure secret provider.");
    }
}

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.UseInMemoryDatabase("TrueCompareTesting");
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = builder.Configuration.GetValue("Security:RequireConfirmedEmail", false);
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 10;
        options.Password.RequiredUniqueChars = 4;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var authenticationBuilder = builder.Services.AddAuthentication();
if (!string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientSecret"]))
{
    authenticationBuilder.AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.ClaimActions.MapJsonKey("email_verified", "email_verified");
    });
}

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddLocalization();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
    {
        if (IPAddress.TryParse(proxy, out var address))
        {
            options.KnownProxies.Add(address);
        }
    }
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            BuildRateLimitPartitionKey(context, "auth"),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
    options.AddPolicy("billing", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            BuildRateLimitPartitionKey(context, "billing"),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 12,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
    options.AddPolicy("alerts", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            BuildRateLimitPartitionKey(context, "alerts"),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
    options.AddPolicy("search", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            BuildRateLimitPartitionKey(context, "search"),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
    options.AddPolicy("image-analysis", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            BuildRateLimitPartitionKey(context, "image-analysis"),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection("Llm"));
builder.Services.Configure<StoreOfferValidationOptions>(builder.Configuration.GetSection("StoreOfferValidation"));
builder.Services.Configure<SearchQuotaOptions>(builder.Configuration.GetSection("SearchQuota"));
builder.Services.Configure<PriceAlertOptions>(builder.Configuration.GetSection("PriceAlerts"));
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = isDevelopment;
    });
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("pt-PT"),
        new CultureInfo("pt"),
        new CultureInfo("en-US"),
        new CultureInfo("en")
    };

    options.DefaultRequestCulture = new RequestCulture("pt-PT");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.ApplyCurrentCultureToResponseHeaders = true;
});
builder.Services.AddSingleton<AppText>();
builder.Services.AddSingleton<ComparisonDataService>();
builder.Services.AddScoped<ProductConversationService>();
builder.Services.AddScoped<SearchMarketContextService>();
builder.Services.AddScoped<SearchQuotaService>();
builder.Services.AddScoped<SearchResultSnapshotService>();
builder.Services.AddScoped<SearchOrchestratorService>();
builder.Services.AddScoped<TargetPriceAlertService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddSingleton<LlmProviderQuotaService>();
builder.Services.AddSingleton<StoreValidationLimiter>();
builder.Services.AddSingleton<ExpensiveOperationLimiter>();
builder.Services.AddHttpClient<LlmProviderRouter>();
builder.Services.AddScoped<IProductDiscoveryService, LlmProductDiscoveryService>();
builder.Services.AddScoped<IProductSuggestionService, LlmSuggestionService>();
builder.Services.AddScoped<IProductImageSuggestionService, LlmProductImageSuggestionService>();
if (builder.Environment.IsEnvironment("Testing") || builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IStoreOfferValidationService, TestingStoreOfferValidationService>();
}
else
{
    builder.Services.AddHttpClient<IStoreOfferValidationService, StoreOfferValidationService>((serviceProvider, client) =>
    {
        var validationOptions = serviceProvider.GetRequiredService<IOptions<StoreOfferValidationOptions>>().Value;
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(validationOptions.RequestTimeoutSeconds, 2, 30));
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false
    });
}
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<SearchHistoryService>();
builder.Services.AddScoped<UserSettingsService>();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<SearchMaintenanceService>();
    builder.Services.AddHostedService<TargetPriceMonitorService>();
}

var app = builder.Build();

await ApplyDatabaseMigrationsAndSeedAsync(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["X-Frame-Options"] = "DENY";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Content-Security-Policy"] = BuildContentSecurityPolicy(app.Environment);

        return Task.CompletedTask;
    });

    await next();
});

app.UseStaticFiles();
app.UseRequestLocalization();
app.Use(async (context, next) =>
{
    var requestCulture = context.Features.Get<IRequestCultureFeature>()?.RequestCulture;
    if (requestCulture is not null
        && (context.Request.Query.ContainsKey("culture") || context.Request.Query.ContainsKey("ui-culture")))
    {
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(requestCulture),
            new CookieOptions
            {
                HttpOnly = false,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = !app.Environment.IsDevelopment()
            });
    }

    await next();
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();


static string BuildContentSecurityPolicy(IHostEnvironment environment)
{
    if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
    {
        return "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; object-src 'none'; img-src 'self' data: https:; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; connect-src 'self' http://localhost:* https://localhost:* ws://localhost:* wss://localhost:* http://127.0.0.1:* https://127.0.0.1:* wss: https:; form-action 'self' https://checkout.stripe.com";
    }

    return "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; object-src 'none'; img-src 'self' data: https:; style-src 'self' 'unsafe-inline'; script-src 'self'; connect-src 'self' wss: https://checkout.stripe.com; form-action 'self' https://checkout.stripe.com";
}

static string BuildRateLimitPartitionKey(HttpContext context, string policyName)
{
    var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    var subject = !string.IsNullOrWhiteSpace(userId)
        ? $"user:{userId}"
        : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "/";
    return $"{policyName}:{subject}:{path}";
}

static async Task ApplyDatabaseMigrationsAndSeedAsync(WebApplication app)
{
    if (app.Environment.IsEnvironment("Testing"))
    {
        return;
    }

    var applyMigrations = app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", app.Environment.IsDevelopment());
    if (!applyMigrations)
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var roleName in new[] { "Admin", "Manager", "User" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to seed role '{roleName}': {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
                }
            }
        }

        var adminEmail = app.Configuration["AdminSeed:Email"];
        var adminPassword = app.Configuration["AdminSeed:Password"];
        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var adminUser = await userManager.FindByEmailAsync(adminEmail.Trim());
            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail.Trim(),
                    Email = adminEmail.Trim(),
                    EmailConfirmed = true,
                    DisplayName = app.Configuration["AdminSeed:DisplayName"]
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to seed admin user: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
                }
            }

            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to assign admin role: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
                }
            }
        }

        logger.LogInformation("Database migrations and seed completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "An error occurred while applying database migrations or seed data.");
        throw;
    }
}

public partial class Program
{
}
