using Bar_QR.Data;
using Bar_QR.Models;
using Microsoft.EntityFrameworkCore;

namespace Bar_QR.Services;

/// <summary>
/// Genera los trabajos de impresión (TrabajoPrint) a partir de los pedidos de una mesa:
/// – Comandas separadas Barra / Cocina (o fusionadas si hay una sola impresora "Todas")
/// – Factura Proforma
/// – Factura Simplificada
/// </summary>
public class PrintService
{
	private readonly AppDbContext _db;
	private readonly string _cabecera;

	public PrintService(AppDbContext db, IConfiguration cfg)
	{
		_db = db;
		_cabecera = cfg["Ticket:Cabecera"] ?? "Bar_QR\nTicket de consumo";
	}

	public async Task EnolarCuentaAsync(int mesaId, string mesaNombre, int numeroMesa, string zonaNombre)
	{
		var bytes = EscPosService.GenerarSolicitudCuenta(zonaNombre, mesaNombre, numeroMesa);
		await GuardarTrabajoFacturaAsync(TipoTrabajoPrint.SolicitudCuenta, bytes,
			$"Cuenta – Mesa {mesaNombre}");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Comandas
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Genera trabajos de comanda para las líneas de un pedido.
	/// Separa automáticamente en Barra y Cocina; si sólo hay impresora "Todas",
	/// ambos tickets van al mismo destino.
	/// </summary>
	public async Task EnolarComandasAsync(int pedidoId)
	{
		var pedido = await _db.PedidosMesa
			.Include(p => p.Mesa).ThenInclude(m => m!.Zona)
			.Include(p => p.Lineas).ThenInclude(l => l.Producto)
			.FirstOrDefaultAsync(p => p.Id == pedidoId);

		if (pedido is null || !pedido.Lineas.Any()) return;

		var zonaLabel  = pedido.Mesa?.Zona?.Nombre ?? "";
		var mesaNombre = pedido.Mesa?.Nombre ?? $"{pedido.MesaId}";
		int mesa       = pedido.MesaId;

		var todasLineas = pedido.Lineas.Where(l => l.Producto != null).ToList();
		var barra  = todasLineas.Where(l => l.Producto!.DestinoImpresion == DestinoImpresion.Barra).ToList();
		var cocina = todasLineas.Where(l => l.Producto!.DestinoImpresion == DestinoImpresion.Cocina).ToList();

		// Si hay impresora específica de Barra/Cocina, separar; si solo hay "Todas", mandar todo junto
		bool hayImpresoraBarra  = await _db.Impresoras.AnyAsync(i => i.Activa && i.Rol == RolImpresora.Barra);
		bool hayImpresoraCocina = await _db.Impresoras.AnyAsync(i => i.Activa && i.Rol == RolImpresora.Cocina);

		if (!hayImpresoraBarra && !hayImpresoraCocina)
		{
			// Solo impresora "Todas": un único ticket con todos los productos
			if (todasLineas.Any())
			{
				var bytes = EscPosService.GenerarComanda(mesa, mesaNombre, zonaLabel, "PEDIDO",
					todasLineas.Select(l => (l.Producto!.Nombre, l.Cantidad)));
				await GuardarTrabajoAsync(TipoTrabajoPrint.ComandaBarra, RolImpresora.Todas, bytes,
					$"Mesa {mesaNombre} – Pedido #{pedidoId}");
			}
			return;
		}

		if (barra.Any())
		{
			var bytes = EscPosService.GenerarComanda(mesa, mesaNombre, zonaLabel, "BARRA",
				barra.Select(l => (l.Producto!.Nombre, l.Cantidad)));
			await GuardarTrabajoAsync(TipoTrabajoPrint.ComandaBarra, RolImpresora.Barra, bytes,
				$"Mesa {mesaNombre} – Pedido #{pedidoId}");
		}

		if (cocina.Any())
		{
			var bytes = EscPosService.GenerarComanda(mesa, mesaNombre, zonaLabel, "COCINA",
				cocina.Select(l => (l.Producto!.Nombre, l.Cantidad)));
			await GuardarTrabajoAsync(TipoTrabajoPrint.ComandaCocina, RolImpresora.Cocina, bytes,
				$"Mesa {mesaNombre} – Pedido #{pedidoId}");
		}
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Factura Proforma
	// ─────────────────────────────────────────────────────────────────────────

	public async Task EnolarProformaAsync(int mesaId)
	{
		var lineas = await ObtenerLineasAgrupadasAsync(mesaId);
		if (!lineas.Any()) return;

		var mesa = await _db.Mesas.FindAsync(mesaId);
		var zonaId = mesa?.ZonaId ?? 0;
		var mesaNombre = mesa?.Nombre ?? $"{mesaId}";
		var subtotal = lineas.Sum(l => l.Cantidad * l.Precio);
		var (lineasConPluses, total) = await AplicarPlusesAsync(lineas, subtotal, zonaId);
		var bytes = EscPosService.GenerarProforma(_cabecera, mesaId, mesaNombre, lineasConPluses, total);
		await GuardarTrabajoFacturaAsync(TipoTrabajoPrint.Proforma, bytes, $"Mesa {mesaNombre} – Proforma");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Factura Simplificada
	// ─────────────────────────────────────────────────────────────────────────

	public async Task EnolarFacturaSimpleAsync(int mesaId, MetodoPago metodoPago)
	{
		var lineas = await ObtenerLineasAgrupadasAsync(mesaId);
		if (!lineas.Any()) return;

		var mesa = await _db.Mesas.FindAsync(mesaId);
		var zonaId = mesa?.ZonaId ?? 0;
		var mesaNombre = mesa?.Nombre ?? $"{mesaId}";
		var subtotal = lineas.Sum(l => l.Cantidad * l.Precio);
		var (lineasConPluses, total) = await AplicarPlusesAsync(lineas, subtotal, zonaId);
		var bytes = EscPosService.GenerarFacturaSimple(_cabecera, mesaId, mesaNombre, lineasConPluses, total, metodoPago);
		await GuardarTrabajoFacturaAsync(TipoTrabajoPrint.FacturaSimple, bytes, $"Mesa {mesaNombre} – Factura Simple");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Helpers
	// ─────────────────────────────────────────────────────────────────────────

	private async Task<List<(string Nombre, int Cantidad, decimal Precio)>> ObtenerLineasAgrupadasAsync(int mesaId)
	{
		var pedidos = await _db.PedidosMesa
			.Include(p => p.Lineas).ThenInclude(l => l.Producto)
			.Where(p => p.MesaId == mesaId)
			.ToListAsync();

		return pedidos
			.SelectMany(p => p.Lineas)
			.GroupBy(l => l.ProductoId)
			.Select(g => (
				Nombre:   g.First().Producto!.Nombre,
				Cantidad: g.Sum(l => l.Cantidad),
				Precio:   g.First().PrecioOverride ?? g.First().Producto!.Precio
			))
				.ToList();
			}

			/// <summary>
			/// Añade líneas de plus al listado y devuelve el total final con pluses aplicados.
			/// Solo aplica pluses activos cuyo DiasJson incluya el día actual (o estén vacíos = todos los días).
			/// </summary>
			private async Task<(List<(string Nombre, int Cantidad, decimal Precio)> Lineas, decimal Total)>
				AplicarPlusesAsync(List<(string Nombre, int Cantidad, decimal Precio)> lineas, decimal subtotal, int zonaId)
			{
				var hoy = (int)DateTime.Now.DayOfWeek;
				var pluses = await _db.Pluses.Where(p => p.Activo && p.ZonaId == zonaId).ToListAsync();
				var result = new List<(string, int, decimal)>(lineas);
				decimal total = subtotal;

				foreach (var plus in pluses)
				{
					List<int> dias;
					try { dias = System.Text.Json.JsonSerializer.Deserialize<List<int>>(plus.DiasJson) ?? new(); }
					catch { dias = new(); }

					if (dias.Any() && !dias.Contains(hoy)) continue;

					var importe = Math.Round(subtotal * plus.Porcentaje / 100m, 2);
					if (importe == 0) continue;
					result.Add(($"{plus.Nombre} (+{plus.Porcentaje:0.##}%)", 1, importe));
					total += importe;
				}

				return (result, total);
			}

				private async Task GuardarTrabajoAsync(
					TipoTrabajoPrint tipo,
					RolImpresora rolDestino,
					byte[] bytes,
					string referencia)
				{
					// Si sólo existe una impresora con rol "Todas", redirigir ambos destinos a ella
					bool hayEspecifica = await _db.Impresoras
						.AnyAsync(i => i.Activa && (int)i.Rol == (int)rolDestino);

					bool hayTodas = await _db.Impresoras
						.AnyAsync(i => i.Activa && i.Rol == RolImpresora.Todas);

					if (!hayEspecifica && hayTodas)
						rolDestino = RolImpresora.Todas;

					_db.TrabajosPrint.Add(new TrabajoPrint
					{
						Tipo             = tipo,
						Estado           = EstadoTrabajoPrint.Pendiente,
						DestinoRol       = rolDestino,
						CreadoEn         = DateTime.UtcNow,
						ContenidoBase64  = Convert.ToBase64String(bytes),
						Referencia       = referencia
					});
					await _db.SaveChangesAsync();
				}

				/// <summary>
				/// Encola un trabajo de factura (Proforma o FacturaSimple) para todas las impresoras
				/// que tengan ImprimeFacturas=true. Si no hay ninguna, usa cualquier impresora activa con rol Todas.
				/// </summary>
				private async Task GuardarTrabajoFacturaAsync(TipoTrabajoPrint tipo, byte[] bytes, string referencia)
				{
					var impresoras = await _db.Impresoras
						.Where(i => i.Activa && i.ImprimeFacturas)
						.ToListAsync();

					// Fallback: si no hay ninguna con ImprimeFacturas, enrutar a rol Todas (comportamiento anterior)
					if (!impresoras.Any())
					{
						await GuardarTrabajoAsync(tipo, RolImpresora.Todas, bytes, referencia);
						return;
					}

					// Crear un trabajo por cada impresora configurada para facturas
					foreach (var imp in impresoras)
					{
						_db.TrabajosPrint.Add(new TrabajoPrint
						{
							Tipo            = tipo,
							Estado          = EstadoTrabajoPrint.Pendiente,
							DestinoRol      = imp.Rol,
							CreadoEn        = DateTime.UtcNow,
							ContenidoBase64 = Convert.ToBase64String(bytes),
							Referencia      = referencia
						});
					}
					await _db.SaveChangesAsync();
				}
			}
