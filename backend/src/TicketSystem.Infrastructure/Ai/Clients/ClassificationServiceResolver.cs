using Microsoft.Extensions.Configuration;
using TicketSystem.Application.Common.Interfaces;

namespace TicketSystem.Infrastructure.Ai.Clients;

public sealed class ClassificationServiceResolver(
    IConfiguration configuration,
    ClaudeClassificationService claudeService,
    OpenRouterClassificationService openRouterService,
    KeywordBasedClassificationService keywordService) : IClassificationServiceResolver
{
    public IClassificationService Resolve(string? provider)
    {
        var selectedProvider = provider ?? configuration["Classification:Provider"] ?? "OpenRouter";
        if (string.Equals(selectedProvider, "OpenRouter", StringComparison.OrdinalIgnoreCase))
            return openRouterService;

        if (string.Equals(selectedProvider, "Claude", StringComparison.OrdinalIgnoreCase))
            return claudeService;

        if (string.Equals(selectedProvider, "Keyword", StringComparison.OrdinalIgnoreCase))
            return keywordService;

        throw new ArgumentException("Provider muss 'OpenRouter', 'Claude' oder 'Keyword' sein.", nameof(provider));
    }
}
