using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebPepperCan.Data;
using Microsoft.Azure.SignalR;
using WebPepperCan.Hubs;
using WebPepperCan.Services;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

try 
{
    var builder = WebApplication.CreateBuilder(args);

    // Hata ayıklama için
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddFilter("Microsoft.Azure.SignalR", LogLevel.Warning);
    builder.Logging.AddDebug();
    builder.Logging.SetMinimumLevel(LogLevel.Debug);

    // Add services to the container.
    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/");
        options.Conventions.AuthorizeFolder("/Admin", "RequireAdminRole");
        options.Conventions.AllowAnonymousToPage("/Account/Login");
    })
    .AddRazorPagesOptions(options =>
    {
        options.Conventions.AuthorizeFolder("/Admin");
    });

    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "Cookies";
    })
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);             // Cookie'nin geçerlilik süresi
        options.SlidingExpiration = true;                            // Her istekte süreyi yenile
    });

    // Add DbContext
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Veritabanı bağlantı dizesi bulunamadı!");
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString)
    );

    builder.Services.AddSignalR().AddAzureSignalR(options =>
    {
        options.ConnectionString = builder.Configuration["AzureSignalRConnectionString"];
        options.ServerStickyMode = Microsoft.Azure.SignalR.ServerStickyMode.Required;
    });

    builder.Services.AddRazorPages();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<UserActivityService>();
    builder.Services.AddScoped<IUserActivityService, UserActivityService>();
    builder.Services.AddScoped<IUserSessionService, UserSessionService>();

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy =>
            policy.RequireRole("Admin"));
    });

    // Key Vault bağlantısı
    var keyVaultEndpoint = builder.Configuration["KeyVaultEndpoint"];
    if (!string.IsNullOrEmpty(keyVaultEndpoint))
    {
        try
        {
            var secretClient = new SecretClient(
                new Uri(keyVaultEndpoint),
                new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    Retry = { MaxRetries = 3, NetworkTimeout = TimeSpan.FromSeconds(5) }
                }));

            // Key Vault'tan gelen değerleri Configuration'a ekle
            foreach (var secret in secretClient.GetPropertiesOfSecrets())
            {
                try
                {
                    var secretValue = secretClient.GetSecret(secret.Name).Value;
                    builder.Configuration[secret.Name] = secretValue.Value;
                }
                catch (Exception secretEx)
                {
                    var logger = builder.Services.BuildServiceProvider()
                        .GetRequiredService<ILogger<Program>>();
                    logger.LogError(secretEx, "Secret değeri alınırken hata: {SecretName}", secret.Name);
                }
            }
        }
        catch (Exception kvEx)
        {
            var logger = builder.Services.BuildServiceProvider()
                .GetRequiredService<ILogger<Program>>();
            logger.LogError(kvEx, "Key Vault bağlantı hatası: {Endpoint}", keyVaultEndpoint);
        }
    }

    // SignalR bağlantısını kontrol et
    var signalRConnection = builder.Configuration["AzureSignalRConnectionString"];
    if (string.IsNullOrEmpty(signalRConnection))
    {
        throw new InvalidOperationException("SignalR bağlantı dizesi bulunamadı!");
    }

    // Activity Service kaydı
    builder.Services.AddScoped<IActivityService, ActivityService>();

    // Güvenlik politikaları
    builder.Services.AddAntiforgery(options => {
        options.HeaderName = "X-CSRF-TOKEN";
    });

    builder.Services.AddScoped<IOrganizationService, OrganizationService>();

    builder.Services.AddMvc();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    // Geçici olarak production'da da detaylı hata sayfasını aktif edelim
    app.UseDeveloperExceptionPage();

    // Statik dosyalar için özel hata yakalama
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            if (ctx.File.Name == "favicon.ico")
            {
                // Favicon bulunamazsa varsayılan bir favicon kullan
                if (!File.Exists(Path.Combine(app.Environment.WebRootPath, "favicon.ico")))
                {
                    ctx.Context.Response.Redirect("/images/default-favicon.ico");
                }
            }
        }
    });

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();    // Önce Authentication
    app.UseAuthorization();     // Sonra Authorization

    app.MapRazorPages();
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // Settings route'unu ekle
    app.MapControllerRoute(
        name: "settings",
        pattern: "Settings",
        defaults: new { controller = "Settings", action = "Index" }
    );

    app.MapGet("/", async context => {
        if (!context.User.Identity.IsAuthenticated)
        {
            context.Response.Redirect("/Account/Login", true); // true parametresi permanent redirect için
            return;
        }
        context.Response.Redirect("/Dashboard", true);
    });

    app.MapControllerRoute(
        name: "vehicleDetails",
        pattern: "VehicleDetails",
        defaults: new { controller = "VehicleDetails", action = "Index" }
    );

    app.MapControllerRoute(
        name: "charts",
        pattern: "Charts",
        defaults: new { controller = "Charts", action = "Index" }
    );

    app.MapHub<MessageHub>("/messageHub");

    app.Run();
}
catch (Exception ex)
{
    var logPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
    Directory.CreateDirectory(logPath);
    var logFile = Path.Combine(logPath, $"startup_error_{DateTime.UtcNow:yyyyMMddHHmmss}.log");
    
    var errorMessage = $@"
Uygulama başlatma hatası [{DateTime.UtcNow}]
Exception: {ex.Message}
Stack Trace: {ex.StackTrace}
Source: {ex.Source}

Inner Exception: {ex.InnerException?.Message}
Inner Stack Trace: {ex.InnerException?.StackTrace}

Current Directory: {Directory.GetCurrentDirectory()}
";
    
    File.WriteAllText(logFile, errorMessage);
    throw;
}
