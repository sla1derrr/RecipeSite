using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecipeSite.Data;
using RecipeSite.Models;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Увеличиваем общий размер заголовков на уровне Kestrel (защита от HTTP 431)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestHeadersTotalSize = 64 * 1024; // 64 KB
});

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Add services to the container.
var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                        ?? Environment.GetEnvironmentVariable("DATABASE_PRIVATE_URL")
                        ?? builder.Configuration["DATABASE_URL"]
                        ?? builder.Configuration.GetConnectionString("DefaultConnection");

string connectionString = rawConnectionString ?? "";

if (!string.IsNullOrEmpty(rawConnectionString) && (rawConnectionString.StartsWith("postgres://") || rawConnectionString.StartsWith("postgresql://")))
{
    var uri = new Uri(rawConnectionString);
    var userInfo = uri.UserInfo.Split(':');
    var user = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');

    connectionString = $"Host={uri.Host};Port={port};Database={database};Username={user};Password={password};Include Error Detail=true";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Персистентные ключи шифрования cookie в БД (защита от раздувания кук при деплоях)
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("RecipeSite");

// Перенос TempData в сессию, чтобы не перегружать заголовки запроса
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// Ограничиваем параметры и имя кук Identity, чтобы они не разрастались и не вызывали HTTP 431
// Ограничиваем параметры и имя кук Identity, чтобы они не разрастались и не вызывали HTTP 431
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".RecipeSite.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Path = "/";
    options.ExpireTimeSpan = TimeSpan.FromHours(12); // Сокращаем время жизни, чтобы не копились
    options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews()
    .AddSessionStateTempDataProvider();
builder.Services.AddHttpClient<RecipeSite.Services.MealDbService>();
builder.Services.AddHttpClient<RecipeSite.Services.SpoonacularService>();
builder.Services.AddHttpClient<RecipeSite.Services.EdamamService>();
builder.Services.AddHttpClient<RecipeSite.Services.FactsService>();
builder.Services.AddLocalization();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new System.Globalization.CultureInfo("ru"),
        new System.Globalization.CultureInfo("en"),
        new System.Globalization.CultureInfo("uk"),
        new System.Globalization.CultureInfo("pl")
    };

    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("ru");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

// Настройка для корректной работы за прокси Railway
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Блок для аватарок
var webRoot = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var avatarsFolder = Path.Combine(webRoot, "avatars");
Directory.CreateDirectory(avatarsFolder);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(avatarsFolder),
    RequestPath = "/avatars"
});

app.UseRouting();
app.UseRequestLocalization();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();