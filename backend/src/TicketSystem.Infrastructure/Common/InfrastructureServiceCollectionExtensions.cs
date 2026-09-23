using Microsoft.Extensions.DependencyInjection;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Classification.Repositories;
using TicketSystem.Domain.Tickets.Repositories;
using TicketSystem.Infrastructure.Ai.Clients;
using TicketSystem.Infrastructure.Persistence.Repositories;

namespace TicketSystem.Infrastructure.Common;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ITicketRepository, InMemoryTicketRepository>();
        services.AddSingleton<ITicketClassificationRepository, InMemoryTicketClassificationRepository>();
        services.AddSingleton<IClassificationService, KeywordBasedClassificationService>();
        return services;
    }
}
