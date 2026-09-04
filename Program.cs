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

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();
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
    db.Database.EnsureCreated();
}
app.Run();
