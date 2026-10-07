using TicketSystem.Application.Common;
using TicketSystem.Infrastructure.Common;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllers();

app.Run();

static bool IsMissingApiKey(string? apiKey) =>
    string.IsNullOrWhiteSpace(apiKey)
    || string.Equals(apiKey.Trim(), "XXXXX", StringComparison.Ordinal);
