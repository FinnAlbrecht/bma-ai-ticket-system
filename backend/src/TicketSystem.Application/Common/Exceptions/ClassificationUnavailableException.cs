namespace TicketSystem.Application.Common.Exceptions;

public sealed class ClassificationUnavailableException : Exception
{
    public ClassificationUnavailableException()
        : base("Die Klassifizierung ist derzeit nicht verfügbar.")
    {
    }
}
