using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecipeSite.Data;
using RecipeSite.Models;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

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

// Персистентные ключи шифрования cookie в БД, а не на эфемерном диске контейнера.
// Без этого при каждом рестарте/редеплое на Railway генерируются новые ключи,
// старые cookie не расшифровываются, и они начинают копиться/раздувать заголовки -> HTTP 431.
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("RecipeSite");

// TempData по умолчанию хранится в cookie (CookieTempDataProvider).
// Переносим в сессию, чтобы статусные сообщения (после логина, смены пароля и т.п.)
// не добавляли вес в заголовки запроса.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// === ИСПРАВЛЕННЫЙ БЛОК IDENTITY ===
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true; // Запрещает регистрацию с одинаковым email
})
.AddEntityFrameworkStores<ApplicationDbContext>();
// ==================================

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();

// === ДОБАВЛЕННЫЙ БЛОК ДЛЯ АВАТАРОК ===
var webRoot = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var avatarsFolder = Path.Combine(webRoot, "avatars");
Directory.CreateDirectory(avatarsFolder); // Гарантируем, что папка существует

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(avatarsFolder),
    RequestPath = "/avatars"
});
// =====================================

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