using TicketSystem.Application.Common;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
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
var databasePath = Path.Combine(builder.Environment.ContentRootPath, "tickets.db");
var databaseConnectionString = builder.Configuration.GetConnectionString("TicketDatabase");
if (string.IsNullOrWhiteSpace(databaseConnectionString))
    databaseConnectionString = $"Data Source={databasePath}";
builder.Services.AddInfrastructure(builder.Configuration, databaseConnectionString);

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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static bool IsMissingApiKey(string? apiKey) =>
    string.IsNullOrWhiteSpace(apiKey)
    || string.Equals(apiKey.Trim(), "XXXXX", StringComparison.Ordinal);
