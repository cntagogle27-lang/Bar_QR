




using Bar_QR.Models;
using Microsoft.EntityFrameworkCore;

namespace Bar_QR.Data;

public static class DbInitializer
{
	public static void Initialize(AppDbContext context)
	{
		// ── PASO 1: Crear esquema base en SQL puro (idempotente, nunca falla) ──────
		// Esto garantiza que todas las tablas y columnas existen ANTES de que EF las use,
		// independientemente del estado de las migraciones.
		try
		{
			// Historial de migraciones
			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
					MigrationId TEXT NOT NULL PRIMARY KEY,
					ProductVersion TEXT NOT NULL
				)");

			// Tablas principales (si no existen)
			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS Productos (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					Nombre TEXT NOT NULL,
					Precio REAL NOT NULL DEFAULT 0,
					Grupo INTEGER NOT NULL DEFAULT 0,
					DestinoImpresion INTEGER NOT NULL DEFAULT 0,
					FotoUrl TEXT NULL,
					FotoData BLOB NULL,
					FotoMimeType TEXT NULL
				)");

			// Añadir FotoUrl si la tabla ya existía sin ella
			try { context.Database.ExecuteSqlRaw("ALTER TABLE Productos ADD COLUMN FotoUrl TEXT NULL"); }
			catch { /* Ya existe */ }

			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS Mesas (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					NumeroMesa INTEGER NOT NULL,
					Nombre TEXT NOT NULL DEFAULT '',
					Slug TEXT NOT NULL UNIQUE,
					Estado INTEGER NOT NULL DEFAULT 0,
					ZonaId INTEGER NULL,
					PosX INTEGER NOT NULL DEFAULT 20,
					PosY INTEGER NOT NULL DEFAULT 20,
					Ancho INTEGER NOT NULL DEFAULT 100,
					Alto INTEGER NOT NULL DEFAULT 80
				)");

			// Añadir columnas nuevas a Mesas si vienen de BD antigua (ALTER TABLE idempotente)
			foreach (var (col, tipo) in new[] {
				("Nombre", "TEXT NOT NULL DEFAULT ''"),
				("Slug", "TEXT NOT NULL DEFAULT ''"),
				("Estado", "INTEGER NOT NULL DEFAULT 0"),
				("ZonaId", "INTEGER NULL"),
				("PosX", "INTEGER NOT NULL DEFAULT 20"),
				("PosY", "INTEGER NOT NULL DEFAULT 20"),
				("Ancho", "INTEGER NOT NULL DEFAULT 100"),
				("Alto", "INTEGER NOT NULL DEFAULT 80"),
				("Habilitada", "INTEGER NOT NULL DEFAULT 1"),
			})
			{
				try { context.Database.ExecuteSqlRaw($"ALTER TABLE Mesas ADD COLUMN {col} {tipo}"); }
				catch { /* Ya existe */ }
			}

			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS Zonas (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					Nombre TEXT NOT NULL,
					Habilitada INTEGER NOT NULL DEFAULT 1
				)");
			try { context.Database.ExecuteSqlRaw("ALTER TABLE Zonas ADD COLUMN Habilitada INTEGER NOT NULL DEFAULT 1"); } catch { }

			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS Empleados (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					Nombre TEXT NOT NULL,
					AvatarTipo TEXT NOT NULL DEFAULT 'avatar_h1',
					FotoData BLOB NULL,
					FotoMime TEXT NULL,
					Pin TEXT NULL,
					Rol TEXT NOT NULL DEFAULT 'Camarero'
				)");

			// Columnas nuevas para BDs existentes
			try { context.Database.ExecuteSqlRaw("ALTER TABLE Empleados ADD COLUMN Pin TEXT NULL"); } catch { }
			try { context.Database.ExecuteSqlRaw("ALTER TABLE Empleados ADD COLUMN Rol TEXT NOT NULL DEFAULT 'Camarero'"); } catch { }

			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS StaffEmails (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					Email TEXT NOT NULL
				)");

			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS ProxyIps (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					IpOrCidr TEXT NOT NULL
				)");

			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS SiteTokens (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					Token TEXT NOT NULL
				)");

			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS SesionesMesa (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					MesaId INTEGER NOT NULL,
					Token TEXT NOT NULL UNIQUE,
					Expira TEXT NOT NULL,
					FOREIGN KEY (MesaId) REFERENCES Mesas(Id) ON DELETE CASCADE
				)");

			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS PedidosMesa (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					MesaId INTEGER NOT NULL,
					CreadoEn TEXT NOT NULL DEFAULT '',
					Estado INTEGER NOT NULL DEFAULT 0,
					FOREIGN KEY (MesaId) REFERENCES Mesas(Id) ON DELETE CASCADE
				)");

			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS LineasPedido (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					PedidoMesaId INTEGER NOT NULL,
					ProductoId INTEGER NOT NULL,
					Cantidad INTEGER NOT NULL DEFAULT 1,
					PrecioOverride REAL NULL,
					FOREIGN KEY (PedidoMesaId) REFERENCES PedidosMesa(Id) ON DELETE CASCADE,
					FOREIGN KEY (ProductoId) REFERENCES Productos(Id) ON DELETE CASCADE
				)");
			try { context.Database.ExecuteSqlRaw("ALTER TABLE LineasPedido ADD COLUMN PrecioOverride REAL NULL"); } catch { }

			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS TicketPlantillas (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					CabeceraJson TEXT NULL,
					PieJson TEXT NULL,
					ImprimirHora INTEGER NOT NULL DEFAULT 0,
					ImprimirUsuario INTEGER NOT NULL DEFAULT 0,
					ImprimirImpuestos INTEGER NOT NULL DEFAULT 0,
					ImprimirDesglose INTEGER NOT NULL DEFAULT 0
				)");

			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS TicketImagenes (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					Nombre TEXT NOT NULL DEFAULT '',
					Data BLOB NOT NULL,
					MimeType TEXT NOT NULL DEFAULT 'image/png',
					Zona TEXT NOT NULL DEFAULT 'cabecera'
				)");

			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS DataProtectionKeys (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					FriendlyName TEXT NULL,
					Xml TEXT NULL
				)");

			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS Impresoras (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					Nombre TEXT NOT NULL DEFAULT '',
					Direccion TEXT NOT NULL DEFAULT '',
					Rol INTEGER NOT NULL DEFAULT 2,
					Activa INTEGER NOT NULL DEFAULT 1
				)");

			context.Database.ExecuteSqlRaw(@"
				CREATE TABLE IF NOT EXISTS TrabajosPrint (
					Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					Tipo INTEGER NOT NULL DEFAULT 0,
					Estado INTEGER NOT NULL DEFAULT 0,
					DestinoRol INTEGER NOT NULL DEFAULT 2,
					CreadoEn TEXT NOT NULL,
					ImprestoEn TEXT NULL,
					ContenidoBase64 TEXT NOT NULL DEFAULT '',
					Referencia TEXT NOT NULL DEFAULT ''
				)");

			// Columna DestinoImpresion en Productos (0=Barra, 1=Cocina)
			try { context.Database.ExecuteSqlRaw("ALTER TABLE Productos ADD COLUMN DestinoImpresion INTEGER NOT NULL DEFAULT 0"); } catch { }

			// Registrar todas las migraciones como aplicadas
			foreach (var m in new[] {
				"20260516105820_InitialCreate",
				"20260516113311_AddMesaNombreSlug",
				"20260520152828_AddDataProtectionKeys",
				"20260526190241_AddGrupoAndFotoToProducto",
				"20260526210337_AddFotoDataToProducto",
				"20260527172911_AddTicketPlantilla",
				"20260528234008_EmpleadosYZonas",
			})
			{
				context.Database.ExecuteSqlRaw(
					$"INSERT OR IGNORE INTO __EFMigrationsHistory VALUES ('{m}','9.0.0')");
			}

			Console.WriteLine("[DbInit] Esquema SQL directo OK.");
		}
		catch (Exception exSql)
		{
			Console.Error.WriteLine($"[DbInit] Error esquema SQL: {exSql.Message}");
		}

		// ── PASO 2: Migrate() para cualquier migración pendiente futura ──────────
		try
		{
			context.Database.Migrate();
			Console.WriteLine("[DbInit] Migrate() OK.");
		}
		catch (Exception exMig)
		{
			Console.Error.WriteLine($"[DbInit] Migrate() (ignorado): {exMig.Message}");
		}

		// ── PASO 3: Seed de datos iniciales ──────────────────────────────────────
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

			if (!context.Zonas.Any())
			{
				var salon = new Zona { Nombre = "Salón" };
				context.Zonas.Add(salon);
				context.SaveChanges();
				foreach (var m in context.Mesas.Where(m => m.ZonaId == null))
					m.ZonaId = salon.Id;
				context.SaveChanges();
			}

			Console.WriteLine("[DbInit] Seeds OK.");
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[DbInit] Error en seeds: {ex.Message}");
		}
	}
}
