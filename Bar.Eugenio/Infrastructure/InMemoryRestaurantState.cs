using Bar.Eugenio.Domain.Menu;
using Bar.Eugenio.Domain.Roles;
using Bar.Eugenio.Domain.Staff;
using Bar.Eugenio.Domain.Tables;

namespace Bar.Eugenio.Infrastructure;

public sealed class InMemoryRestaurantState
{
    public object Gate { get; } = new();

    public List<MenuProduct> Products { get; } =
    [
        new() { Id = 1, Name = "Cafe espresso", Price = 1.50m, Category = "Bebidas" },
        new() { Id = 2, Name = "Croissant", Price = 2.80m, Category = "Comida" },
        new() { Id = 3, Name = "Hamburguesa", Price = 8.50m, Category = "Comida" },
        new() { Id = 4, Name = "Cerveza", Price = 3.00m, Category = "Bebidas" },
        new() { Id = 5, Name = "Tarta de queso", Price = 4.20m, Category = "Postres" }
    ];

    public List<StaffMember> Staff { get; } =
    [
        new() { Id = 1, Name = "Paco", Role = StaffRole.Empleado },
        new() { Id = 2, Name = "Luis", Role = StaffRole.Empleado },
        new() { Id = 3, Name = "Marta", Role = StaffRole.Encargado, Pin = "4321" },
        new() { Id = 4, Name = "Admin", Role = StaffRole.Admin, Pin = "1234" }
    ];

    public List<RestaurantTable> Tables { get; set; } = Enumerable.Range(1, 12)
        .Select(number => new RestaurantTable(number))
        .ToList();

    public int NextOrderItemId { get; set; } = 1;
}
