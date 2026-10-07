using Anthropic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Infrastructure.Ai;
using TicketSystem.Infrastructure.Ai.Clients;
using TicketSystem.Domain.Classification.Repositories;
using TicketSystem.Domain.Tickets.Repositories;
using TicketSystem.Infrastructure.Persistence.Repositories;

namespace TicketSystem.Infrastructure.Common;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AnthropicOptions>(configuration.GetSection(AnthropicOptions.SectionName));
        services.Configure<OpenRouterOptions>(configuration.GetSection(OpenRouterOptions.SectionName));
        services.AddSingleton<ITicketRepository, InMemoryTicketRepository>();
        services.AddSingleton<ITicketClassificationRepository, InMemoryTicketClassificationRepository>();
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AnthropicOptions>>().Value;
            return new AnthropicClient
            {
                ApiKey = options.ApiKey ?? string.Empty,
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
            };
        });
        services.AddSingleton<IAnthropicMessageClient, AnthropicMessageClient>();
        services.AddHttpClient<IOpenRouterMessageClient, OpenRouterMessageClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddScoped<ClaudeClassificationService>();
        services.AddScoped<OpenRouterClassificationService>();
        services.AddScoped<KeywordBasedClassificationService>();
        services.AddScoped<IClassificationServiceResolver, ClassificationServiceResolver>();
        return services;
    }
}
