using Microsoft.Extensions.DependencyInjection;
using TicketSystem.Application.Classification.Commands;
using TicketSystem.Application.Tickets.Commands;
using TicketSystem.Application.Tickets.Queries;

namespace TicketSystem.Application.Common;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateTicketCommandHandler>();
        services.AddScoped<GetAllTicketsQueryHandler>();
        services.AddScoped<GetTicketByIdQueryHandler>();
        services.AddScoped<ClassifyTicketCommandHandler>();
        return services;
    }
}
