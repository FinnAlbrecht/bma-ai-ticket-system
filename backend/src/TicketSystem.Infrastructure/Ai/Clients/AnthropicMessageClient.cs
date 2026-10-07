using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using TicketSystem.Infrastructure.Ai;

namespace TicketSystem.Infrastructure.Ai.Clients;

public sealed class AnthropicMessageClient(AnthropicClient client, IOptions<AnthropicOptions> options) : IAnthropicMessageClient
{
    public async Task<string> CreateMessageAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var response = await client.Messages.Create(
            new MessageCreateParams
            {
                MaxTokens = options.Value.MaxTokens,
                System = systemPrompt,
                Messages =
                [
                    new()
                    {
                        Role = Role.User,
                        Content = userPrompt
                    }
                ],
                Model = options.Value.Model
            },
            cancellationToken: ct);

        var text = new List<string>();
        foreach (var block in response.Content)
        {
            if (block.TryPickText(out var textBlock) && textBlock?.Text is { } blockText)
                text.Add(blockText);
        }

        return string.Join(Environment.NewLine, text);
    }
}
