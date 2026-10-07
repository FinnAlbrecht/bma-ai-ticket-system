using TicketSystem.Application.Common;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Npgsql;
using TicketSystem.Infrastructure.Common;
using TicketSystem.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TicketDesk.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<UserRecord>, PasswordHasher<UserRecord>>();

builder.Services.AddApplication();
var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var databaseConnectionString = builder.Configuration.GetConnectionString("TicketDatabase");
if (!string.IsNullOrWhiteSpace(databaseConnectionString))
    databaseConnectionString = NormalizePostgreSqlConnectionString(databaseConnectionString, databaseProvider);

if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase)
    && string.IsNullOrWhiteSpace(databaseConnectionString))
{
    var databasePath = Path.Combine(builder.Environment.ContentRootPath, "tickets.db");
    databaseConnectionString = $"Data Source={databasePath}";
}
if (string.IsNullOrWhiteSpace(databaseConnectionString))
    throw new InvalidOperationException("ConnectionStrings:TicketDatabase muss für den gewählten Datenbankanbieter gesetzt sein.");
builder.Services.AddInfrastructure(builder.Configuration, databaseConnectionString, databaseProvider);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
    await TicketDatabaseInitializer.InitializeAsync(database);
}

var configuredProvider = builder.Configuration["Classification:Provider"] ?? "OpenRouter";
if (string.Equals(configuredProvider, "OpenRouter", StringComparison.OrdinalIgnoreCase)
    && IsMissingApiKey(builder.Configuration["OpenRouter:ApiKey"]))
{
    app.Logger.LogWarning("OpenRouter API key is missing; classifications will return 503 until it is configured.");
}
else if (string.Equals(configuredProvider, "Claude", StringComparison.OrdinalIgnoreCase)
    && IsMissingApiKey(builder.Configuration["Anthropic:ApiKey"]))
{
    app.Logger.LogWarning("Anthropic API key is missing; classifications will return 503 until it is configured.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("dashboard", "index.html");
app.MapFallbackToFile("tickets/{*path:nonfile}", "index.html");

app.Run();

static bool IsMissingApiKey(string? apiKey) =>
    string.IsNullOrWhiteSpace(apiKey)
    || string.Equals(apiKey.Trim(), "XXXXX", StringComparison.Ordinal);

static string NormalizePostgreSqlConnectionString(string connectionString, string provider)
{
    if (!string.Equals(provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase)
        || !Uri.TryCreate(connectionString, UriKind.Absolute, out var uri)
        || uri.Scheme is not ("postgres" or "postgresql"))
    {
        return connectionString;
    }

    var credentials = uri.UserInfo.Split(':', 2);
    var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
    if (credentials.Length != 2 || string.IsNullOrWhiteSpace(database))
        throw new InvalidOperationException("Die PostgreSQL-URL enthält keinen gültigen Benutzernamen, Passwort oder Datenbanknamen.");

    return new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = database,
        Username = Uri.UnescapeDataString(credentials[0]),
        Password = Uri.UnescapeDataString(credentials[1]),
        SslMode = SslMode.VerifyFull
    }.ConnectionString;
}
