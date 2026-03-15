namespace Numchen.Domain;

public readonly record struct Card(int Value)
{
    public const int MinValue = 1;
    public const int MaxValue = 16;
}
