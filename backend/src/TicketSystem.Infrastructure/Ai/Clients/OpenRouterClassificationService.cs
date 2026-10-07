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
        catch (HttpRequestException ex)
        {
            logger.LogError(
                "OpenRouter request for model {Model} failed with HTTP status {StatusCode} ({ExceptionType}).",
                _options.Model,
                ex.StatusCode is null ? null : (int)ex.StatusCode.Value,
                ex.GetType().Name);
            throw new ClassificationUnavailableException();
        }
        catch (JsonException ex)
        {
            logger.LogError(
                "OpenRouter returned an invalid response for model {Model} ({ExceptionType}).",
                _options.Model,
                ex.GetType().Name);
            throw new ClassificationUnavailableException();
        }
        catch (InvalidDataException ex)
        {
            logger.LogError(
                "OpenRouter returned HTTP success but an unexpected completion format for model {Model}: {Details}",
                _options.Model,
                ex.Message);
            throw new ClassificationUnavailableException();
        }
        catch (TimeoutException ex)
        {
            logger.LogError(
                "OpenRouter request for model {Model} timed out ({ExceptionType}).",
                _options.Model,
                ex.GetType().Name);
            throw new ClassificationUnavailableException();
        }
    }
}
