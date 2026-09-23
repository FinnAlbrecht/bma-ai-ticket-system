namespace TicketSystem.Domain.Classification.ValueObjects;

public readonly record struct ClassificationConfidence
{
    public double Value { get; }

    public ClassificationConfidence(double value)
    {
        if (value < 0 || value > 1)
            throw new ArgumentOutOfRangeException(nameof(value), "Confidence muss zwischen 0 und 1 liegen.");

        Value = value;
    }

    public static implicit operator double(ClassificationConfidence confidence) => confidence.Value;
}
