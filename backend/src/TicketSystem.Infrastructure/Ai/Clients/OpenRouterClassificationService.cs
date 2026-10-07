using System.Diagnostics;
using System.Security;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketSystem.Application.Common.Exceptions;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Classification.Enums;
using TicketSystem.Domain.Tickets.Enums;
using TicketSystem.Infrastructure.Ai;

namespace TicketSystem.Infrastructure.Ai.Clients;

public sealed class OpenRouterClassificationService(
    IOpenRouterMessageClient client,
    IOptions<OpenRouterOptions> options,
    ILogger<OpenRouterClassificationService> logger) : IClassificationService
{
    private readonly OpenRouterOptions _options = options.Value;

    public async Task<ClassificationOutcome> ClassifyAsync(
        string title,
        string description,
        CancellationToken ct = default)
    {
        if (!_options.HasValidApiKey)
        {
            logger.LogError("OpenRouter API key is missing or set to the placeholder.");
            throw new ClassificationUnavailableException();
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await client.CreateMessageAsync(
                ClaudeClassificationService.BuildSystemPrompt(),
                $"<ticket><title>{SecurityElement.Escape(title)}</title><description>{SecurityElement.Escape(description)}</description></ticket>",
                ct);
            stopwatch.Stop();
            return ClaudeClassificationService.ParseResponse(response, _options.Model, stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError("OpenRouter classification request timed out ({ExceptionType}).", ex.GetType().Name);
            throw new ClassificationUnavailableException();
        }
        catch (Exception ex) when (ex is TimeoutException or HttpRequestException or JsonException)
        {
            logger.LogError("OpenRouter classification request failed ({ExceptionType}).", ex.GetType().Name);
            throw new ClassificationUnavailableException();
        }
    }
}
