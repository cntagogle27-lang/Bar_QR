




using Bar_QR.Models;
using Microsoft.EntityFrameworkCore;

namespace Bar_QR.Data;

public static class DbInitializer
{
	public static void Initialize(AppDbContext context)
	{
		// Verificar si hay migraciones en el proyecto
		var allMigrations = context.Database.GetMigrations();

		if (allMigrations.Any())
		{
			// Si hay migraciones definidas, siempre usar el sistema de migraciones
			var pendingMigrations = context.Database.GetPendingMigrations();

			if (pendingMigrations.Any())
			{
				// Aplicar migraciones pendientes
				context.Database.Migrate();
			}
			else
			{
				// Verificar si el esquema existe realmente
				if (!DatabaseSchemaExists(context))
				{
					// Estado inconsistente: eliminar y recrear con migraciones
					context.Database.EnsureDeleted();
					context.Database.Migrate();
				}
			}
		}
	else
		{
			// No hay migraciones: usar EnsureCreated para crear las tablas basándose en el modelo
			// Verificar si el esquema existe
			if (!DatabaseSchemaExists(context))
			{
				// Si no existe el esquema, eliminar todo (incluido __EFMigrationsHistory residual)
				// EnsureCreated() no crea tablas si detecta __EFMigrationsHistory
				context.Database.EnsureDeleted();
			}
			context.Database.EnsureCreated();
		}

		// Seed de datos iniciales
		if (!context.Productos.Any())
		{
			var productos = new List<Producto>
			{
				new Producto { Nombre = "Café Espresso", Precio = 1.50m, Categoria = CategoriaProducto.Bebida, DestinoImpresion = DestinoImpresion.Barra },
				new Producto { Nombre = "Croissant", Precio = 2.80m, Categoria = CategoriaProducto.Comida, DestinoImpresion = DestinoImpresion.Cocina },
				new Producto { Nombre = "Hamburguesa", Precio = 8.50m, Categoria = CategoriaProducto.Comida, DestinoImpresion = DestinoImpresion.Cocina },
				new Producto { Nombre = "Cerveza", Precio = 3.00m, Categoria = CategoriaProducto.Bebida, DestinoImpresion = DestinoImpresion.Barra }
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
				context.Mesas.Add(new Mesa { NumeroMesa = i, Estado = EstadoMesa.Libre });
			}
			context.SaveChanges();
		}
	}

	private static bool DatabaseSchemaExists(AppDbContext context)
	{
		try
		{
			// Verificar directamente en sqlite_master sin acceder a las tablas del modelo
			var connection = context.Database.GetDbConnection();
			var wasOpen = connection.State == System.Data.ConnectionState.Open;

			if (!wasOpen)
				connection.Open();

			using var command = connection.CreateCommand();
			command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name != '__EFMigrationsHistory'";
			var count = Convert.ToInt32(command.ExecuteScalar());

			if (!wasOpen)
				connection.Close();

			// Esperamos al menos 5 tablas: Productos, Mesas, SiteTokens, StaffEmails, ProxyIps
			return count >= 5;
		}
		catch
		{
			return false;
		}
	}
}






