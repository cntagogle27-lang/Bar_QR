using Bar.Eugenio.Domain.Menu;

namespace Bar.Eugenio.Domain.Orders;

public sealed class OrderItem
{
    public OrderItem(int id, MenuProduct product, string createdBy)
    {
        Id = id;
        ProductId = product.Id;
        Name = product.Name;
        OriginalPrice = product.Price;
        TablePrice = product.Price;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.Now;
    }

    public OrderItem(
        int id,
        int productId,
        string name,
        decimal originalPrice,
        decimal tablePrice,
        string createdBy,
        DateTimeOffset createdAt)
    {
        Id = id;
        ProductId = productId;
        Name = name;
        OriginalPrice = originalPrice;
        TablePrice = tablePrice;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public int Id { get; }
    public int ProductId { get; }
    public string Name { get; }
    public decimal OriginalPrice { get; }
    public decimal TablePrice { get; private set; }
    public string CreatedBy { get; }
    public DateTimeOffset CreatedAt { get; }

    public void ChangeTablePrice(decimal newPrice)
    {
        if (newPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newPrice), "El precio no puede ser negativo.");
        }

        TablePrice = newPrice;
    }
}
