namespace Bar.Eugenio.Domain.Menu;

public sealed class MenuProduct
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public decimal Price { get; init; }
    public required string Category { get; init; }
}
