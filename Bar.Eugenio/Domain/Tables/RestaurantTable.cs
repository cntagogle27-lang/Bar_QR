using Bar.Eugenio.Domain.Orders;

namespace Bar.Eugenio.Domain.Tables;

public sealed class RestaurantTable
{
    private readonly List<OrderItem> _items = [];

    public RestaurantTable(int number)
    {
        Number = number;
    }

    public int Number { get; }
    public TableStatus Status { get; private set; } = TableStatus.Libre;
    public string? AccessCode { get; private set; }
    public DateTimeOffset? AccessCodeExpiresAt { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items;
    public decimal Total => _items.Sum(item => item.TablePrice);

    public void Activate(string accessCode, TimeSpan lifetime)
    {
        Status = TableStatus.Activa;
        AccessCode = accessCode;
        AccessCodeExpiresAt = DateTimeOffset.Now.Add(lifetime);
        _items.Clear();
    }

    public void Restore(TableStatus status, string? accessCode, DateTimeOffset? accessCodeExpiresAt, IEnumerable<OrderItem> items)
    {
        Status = status;
        AccessCode = accessCode;
        AccessCodeExpiresAt = accessCodeExpiresAt;
        _items.Clear();
        _items.AddRange(items);
    }

    public void Close()
    {
        Status = TableStatus.Cerrada;
        AccessCode = null;
        AccessCodeExpiresAt = null;
        _items.Clear();
    }

    public bool CanEnterWith(string code)
    {
        return Status == TableStatus.Activa
            && AccessCodeExpiresAt > DateTimeOffset.Now
            && string.Equals(AccessCode, code, StringComparison.OrdinalIgnoreCase);
    }

    public void AddItem(OrderItem item)
    {
        _items.Add(item);
    }

    public bool RemoveItem(int itemId)
    {
        var item = _items.FirstOrDefault(existing => existing.Id == itemId);
        return item is not null && _items.Remove(item);
    }

    public OrderItem? FindItem(int itemId)
    {
        return _items.FirstOrDefault(item => item.Id == itemId);
    }
}
