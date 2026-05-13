namespace Bar.Eugenio.Domain.Menu;

public sealed class MenuProduct
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public required string Category { get; set; }
    public string? ImageUrl { get; set; }
}
