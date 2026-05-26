




using Bar_QR.Models;
using Microsoft.EntityFrameworkCore;

namespace Bar_QR.Data;

public static class DbInitializer
{
	public static void Initialize(AppDbContext context)
	{
		// Aplicar migraciones pendientes. Si la BD ya existe sin historial de migraciones,
		// EnsureCreated se encarga de crear lo que falte sin borrar datos.
		try
		{
			context.Database.Migrate();
		}
		catch
		{
			try { context.Database.EnsureCreated(); }
			catch (Exception ex) { Console.Error.WriteLine($"[DbInit] No se pudo crear la BD: {ex.Message}"); }
		}

		// Seed de datos iniciales
		try
		{
			if (!context.Productos.Any())
{
context.Productos.AddRange(
new Producto { Nombre = "Café Espresso", Precio = 1.50m, Grupo = GrupoProducto.CafeInfusiones, DestinoImpresion = DestinoImpresion.Barra },
new Producto { Nombre = "Cortado", Precio = 1.30m, Grupo = GrupoProducto.CafeInfusiones, DestinoImpresion = DestinoImpresion.Barra },
new Producto { Nombre = "Croissant", Precio = 2.80m, Grupo = GrupoProducto.Desayunos, DestinoImpresion = DestinoImpresion.Cocina },
new Producto { Nombre = "Vino Tinto", Precio = 3.50m, Grupo = GrupoProducto.Vinos, DestinoImpresion = DestinoImpresion.Barra },
new Producto { Nombre = "Cerveza", Precio = 3.00m, Grupo = GrupoProducto.Bebidas, DestinoImpresion = DestinoImpresion.Barra },
new Producto { Nombre = "Ensalada Mixta", Precio = 6.50m, Grupo = GrupoProducto.Ensaladas, DestinoImpresion = DestinoImpresion.Cocina },
new Producto { Nombre = "Croquetas", Precio = 7.00m, Grupo = GrupoProducto.Entrantes, DestinoImpresion = DestinoImpresion.Cocina },
new Producto { Nombre = "Tabla de Quesos", Precio = 10.00m, Grupo = GrupoProducto.Quesos, DestinoImpresion = DestinoImpresion.Cocina },
new Producto { Nombre = "Cochinillo al Horno", Precio = 18.00m, Grupo = GrupoProducto.Horno, DestinoImpresion = DestinoImpresion.Cocina },
new Producto { Nombre = "Entrecot", Precio = 20.00m, Grupo = GrupoProducto.Carnes, DestinoImpresion = DestinoImpresion.Cocina },
new Producto { Nombre = "Tarta de la Casa", Precio = 4.50m, Grupo = GrupoProducto.Postres, DestinoImpresion = DestinoImpresion.Cocina },
new Producto { Nombre = "Gin Tonic", Precio = 8.00m, Grupo = GrupoProducto.LicoresCocteles, DestinoImpresion = DestinoImpresion.Barra }
);
context.SaveChanges();
			}

			if (!context.SiteTokens.Any())
			{
				context.SiteTokens.Add(new SiteToken { Token = "demo-token" });
				context.SaveChanges();
			}

			if (!context.StaffEmails.Any())
			{
				context.StaffEmails.AddRange(
					new StaffEmail { Email = "paco@bar.local" },
					new StaffEmail { Email = "luis@bar.local" }
				);
				context.SaveChanges();
			}

			if (!context.ProxyIps.Any())
			{
				context.ProxyIps.Add(new ProxyIp { IpOrCidr = "127.0.0.1" });
				context.SaveChanges();
			}

			if (!context.Mesas.Any())
			{
				for (int i = 1; i <= 12; i++)
					context.Mesas.Add(new Mesa
					{
						NumeroMesa = i,
						Nombre = $"Mesa {i}",
						Slug = $"mesa-{i}",
						Estado = EstadoMesa.Libre
					});
				context.SaveChanges();
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[DbInit] Error en seed: {ex.Message}");
		}
	}
}


