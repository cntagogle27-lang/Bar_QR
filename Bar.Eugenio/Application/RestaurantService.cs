using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bar.Eugenio.Domain.Menu;
using Bar.Eugenio.Domain.Orders;
using Bar.Eugenio.Domain.Roles;
using Bar.Eugenio.Domain.Staff;
using Bar.Eugenio.Domain.Tables;
using Bar.Eugenio.Infrastructure;

namespace Bar.Eugenio.Application;

public sealed class RestaurantService
{
    private readonly InMemoryRestaurantState _state;
    private readonly SqliteRestaurantStore _store;

    public RestaurantService(InMemoryRestaurantState state, SqliteRestaurantStore store)
    {
        _state = state;
        _store = store;
        _store.Initialize(_state);
    }

    public string IntranetPassword { get; private set; } = "staff";
    public IReadOnlyList<MenuProduct> Products => _state.Products;
    public IReadOnlyList<StaffMember> Staff => _state.Staff;

    public IReadOnlyList<string> ProductCategories
    {
        get
        {
            lock (_state.Gate)
            {
                return _state.Products
                    .Select(product => product.Category)
                    .Where(category => !string.IsNullOrWhiteSpace(category))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(category => category)
                    .ToList();
            }
        }
    }

    public IReadOnlyList<RestaurantTable> GetTables()
    {
        lock (_state.Gate)
        {
            return _state.Tables.ToList();
        }
    }

    public RestaurantTable? FindTable(int number)
    {
        lock (_state.Gate)
        {
            return _state.Tables.FirstOrDefault(table => table.Number == number);
        }
    }

    public bool ValidateIntranetPassword(string password)
    {
        return string.Equals(IntranetPassword, password, StringComparison.Ordinal);
    }

    public bool TrySignInStaff(int staffId, string? pin, out StaffMember? staff, out string message)
    {
        staff = _state.Staff.FirstOrDefault(member => member.Id == staffId);

        if (staff is null)
        {
            message = "No se ha encontrado el empleado.";
            return false;
        }

        if (staff.Role is StaffRole.Encargado or StaffRole.Admin
            && !string.Equals(staff.Pin, pin, StringComparison.Ordinal))
        {
            message = "El PIN no es correcto.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    public string ActivateTable(int tableNumber)
    {
        lock (_state.Gate)
        {
            var table = RequireTable(tableNumber);
            var code = RandomNumberGenerator.GetInt32(0, 10000).ToString("0000", CultureInfo.InvariantCulture);
            table.Activate(code, TimeSpan.FromHours(4));
            _store.Save(_state);
            return code;
        }
    }

    public void CloseTable(int tableNumber)
    {
        lock (_state.Gate)
        {
            RequireTable(tableNumber).Close();
            _store.Save(_state);
        }
    }

    public bool CanCustomerEnter(int tableNumber, string code)
    {
        lock (_state.Gate)
        {
            return RequireTable(tableNumber).CanEnterWith(code);
        }
    }

    public void AddOrderItem(int tableNumber, int productId, string createdBy)
    {
        lock (_state.Gate)
        {
            var table = RequireTable(tableNumber);
            if (table.Status != TableStatus.Activa)
            {
                throw new InvalidOperationException("La mesa no esta activa.");
            }

            var product = _state.Products.First(product => product.Id == productId);
            table.AddItem(new OrderItem(_state.NextOrderItemId++, product, createdBy));
            _store.Save(_state);
        }
    }

    public bool RemoveOrderItem(int tableNumber, int itemId)
    {
        lock (_state.Gate)
        {
            var removed = RequireTable(tableNumber).RemoveItem(itemId);
            if (removed)
            {
                _store.Save(_state);
            }

            return removed;
        }
    }

    public void ChangeItemPrice(int tableNumber, int itemId, decimal newPrice)
    {
        lock (_state.Gate)
        {
            RequireTable(tableNumber).FindItem(itemId)?.ChangeTablePrice(newPrice);
            _store.Save(_state);
        }
    }

    public void SetTableCount(int tableCount)
    {
        if (tableCount < 1 || tableCount > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(tableCount), "El numero de mesas debe estar entre 1 y 200.");
        }

        lock (_state.Gate)
        {
            var next = Enumerable.Range(1, tableCount)
                .Select(number => _state.Tables.FirstOrDefault(table => table.Number == number) ?? new RestaurantTable(number))
                .ToList();

            _state.Tables = next;
            _store.Save(_state);
        }
    }

    public void SaveProducts(IEnumerable<MenuProduct> products)
    {
        lock (_state.Gate)
        {
            var normalized = products
                .Where(product => !string.IsNullOrWhiteSpace(product.Name))
                .Select(product => new MenuProduct
                {
                    Id = product.Id,
                    Name = product.Name.Trim(),
                    Price = product.Price,
                    Category = string.IsNullOrWhiteSpace(product.Category) ? "General" : product.Category.Trim(),
                    ImageUrl = string.IsNullOrWhiteSpace(product.ImageUrl) ? null : product.ImageUrl.Trim()
                })
                .OrderBy(product => product.Id)
                .ToList();

            _state.Products.Clear();
            _state.Products.AddRange(normalized);
            _store.SaveProducts(_state.Products);
        }
    }

    public void SaveStaff(IEnumerable<StaffMember> staff)
    {
        lock (_state.Gate)
        {
            var normalized = staff
                .Where(member => !string.IsNullOrWhiteSpace(member.Name))
                .Select(member => new StaffMember
                {
                    Id = member.Id,
                    Name = member.Name.Trim(),
                    Role = member.Role,
                    Pin = string.IsNullOrWhiteSpace(member.Pin) ? null : member.Pin.Trim(),
                    PhotoUrl = string.IsNullOrWhiteSpace(member.PhotoUrl) ? null : member.PhotoUrl.Trim()
                })
                .OrderBy(member => member.Id)
                .ToList();

            _state.Staff.Clear();
            _state.Staff.AddRange(normalized);
            _store.SaveStaff(_state.Staff);
        }
    }

    public void SetIntranetPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("La contrasena no puede estar vacia.", nameof(password));
        }

        IntranetPassword = password;
    }

    public string BuildQrSvgDataUri(int tableNumber, string baseUri)
    {
        var url = $"{baseUri.TrimEnd('/')}/mesa/{tableNumber}";
        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="180" height="180" viewBox="0 0 180 180">
              <rect width="180" height="180" fill="white"/>
              <text x="90" y="76" font-size="28" font-family="Arial" font-weight="700" text-anchor="middle">Mesa {tableNumber}</text>
              <text x="90" y="110" font-size="13" font-family="Arial" text-anchor="middle">{System.Security.SecurityElement.Escape(url)}</text>
              <rect x="16" y="16" width="40" height="40" fill="#111827"/>
              <rect x="124" y="16" width="40" height="40" fill="#111827"/>
              <rect x="16" y="124" width="40" height="40" fill="#111827"/>
              <path d="M78 126h14v14H78zm24 0h14v14h-14zm0 24h38v14h-38zm38-48h14v38h-14z" fill="#111827"/>
            </svg>
            """;
        return $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(svg))}";
    }

    private RestaurantTable RequireTable(int number)
    {
        return _state.Tables.First(table => table.Number == number);
    }
}
