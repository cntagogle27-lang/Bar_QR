using Bar_QR.Models;

namespace Bar_QR.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        if (!context.Productos.Any())
        {
            var productos = new List<Producto>
            {
                new Producto { Nombre = "Café Espresso", Precio = 1.50m, Categoria = "Bebida", Destino = DestinoPedido.Barra },
                new Producto { Nombre = "Croissant", Precio = 2.80m, Categoria = "Desayuno", Destino = DestinoPedido.Cocina },
                new Producto { Nombre = "Hamburguesa", Precio = 8.50m, Categoria = "Principal", Destino = DestinoPedido.Cocina },
                new Producto { Nombre = "Cerveza", Precio = 3.00m, Categoria = "Bebida", Destino = DestinoPedido.Barra }
            };
            context.Productos.AddRange(productos);
            context.SaveChanges();
        }

        if (!context.SiteTokens.Any())
        {
            context.SiteTokens.AddRange(new SiteToken { Token = "demo-token" });
            context.SaveChanges();
        }

        if (!context.StaffEmails.Any())
        {
            context.StaffEmails.AddRange(new StaffEmail { Email = "paco@bar.local" }, new StaffEmail { Email = "luis@bar.local" });
            context.SaveChanges();
        }

        if (!context.ProxyIps.Any())
        {
            context.ProxyIps.AddRange(new ProxyIp { IpOrCidr = "127.0.0.1" });
            context.SaveChanges();
        }

        if (!context.Mesas.Any())
        {
            for (int i = 1; i <= 12; i++)
            {
                context.Mesas.Add(new Mesa { Numero = i, Ocupada = false });
            }
            context.SaveChanges();
        }
    }
}
