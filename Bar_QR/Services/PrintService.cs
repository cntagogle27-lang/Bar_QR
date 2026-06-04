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

	public PrintService(AppDbContext db)
	{
		_db = db;
	}

	public async Task EnolarCuentaAsync(int mesaId, string mesaNombre, int numeroMesa, string zonaNombre)
	{
		if (!await _db.Impresoras.AnyAsync(i => i.Activa)) return;
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

		var todasLineas = pedido.Lineas.Where(l => l.Producto != null && !l.Impresa).ToList();
		if (!todasLineas.Any()) return;

		bool hayAlgunaImpresora = await _db.Impresoras.AnyAsync(i => i.Activa);
		if (!hayAlgunaImpresora) return;

		bool hayBarra  = await _db.Impresoras.AnyAsync(i => i.Activa && i.Rol == RolImpresora.Barra);
		bool hayCocina = await _db.Impresoras.AnyAsync(i => i.Activa && i.Rol == RolImpresora.Cocina);
		bool hayTodas  = await _db.Impresoras.AnyAsync(i => i.Activa && i.Rol == RolImpresora.Todas);

		var lineasBarra  = todasLineas.Where(l => l.Producto!.DestinoImpresion == DestinoImpresion.Barra).ToList();
		var lineasCocina = todasLineas.Where(l => l.Producto!.DestinoImpresion == DestinoImpresion.Cocina).ToList();

		if (!hayBarra && !hayCocina)
		{
			// Sin impresoras específicas → todo va a "Todas" si existe
			if (hayTodas && todasLineas.Any())
			{
				var bytes = EscPosService.GenerarComanda(mesa, mesaNombre, zonaLabel, "PEDIDO",
					todasLineas.Select(l => (l.Producto!.Nombre, l.Cantidad)));
				await GuardarTrabajoAsync(TipoTrabajoPrint.ComandaBarra, RolImpresora.Todas, bytes,
					$"Mesa {mesaNombre} – Pedido #{pedidoId}");
				foreach (var l in todasLineas) l.Impresa = true;
				await _db.SaveChangesAsync();
			}
			return;
		}

		// Hay impresoras específicas → enrutar estrictamente por destino.
		// Los productos para un destino sin impresora configurada se descartan
		// (se marcan como impresos para no reintentarlos).
		bool saved = false;

		if (lineasBarra.Any())
		{
			if (hayBarra)
			{
				var bytes = EscPosService.GenerarComanda(mesa, mesaNombre, zonaLabel, "BARRA",
					lineasBarra.Select(l => (l.Producto!.Nombre, l.Cantidad)));
				await GuardarTrabajoAsync(TipoTrabajoPrint.ComandaBarra, RolImpresora.Barra, bytes,
					$"Mesa {mesaNombre} – Pedido #{pedidoId}");
			}
			// Si no hay impresora de Barra → descartamos sin imprimir
			foreach (var l in lineasBarra) l.Impresa = true;
			saved = true;
		}

		if (lineasCocina.Any())
		{
			if (hayCocina)
			{
				var bytes = EscPosService.GenerarComanda(mesa, mesaNombre, zonaLabel, "COCINA",
					lineasCocina.Select(l => (l.Producto!.Nombre, l.Cantidad)));
				await GuardarTrabajoAsync(TipoTrabajoPrint.ComandaCocina, RolImpresora.Cocina, bytes,
					$"Mesa {mesaNombre} – Pedido #{pedidoId}");
			}
			// Si no hay impresora de Cocina → descartamos sin imprimir
			foreach (var l in lineasCocina) l.Impresa = true;
			saved = true;
		}

		if (saved) await _db.SaveChangesAsync();
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Factura Proforma
	// ─────────────────────────────────────────────────────────────────────────

	public async Task EnolarProformaAsync(int mesaId)
	{
		if (!await _db.Impresoras.AnyAsync(i => i.Activa)) return;
		var lineas = await ObtenerLineasAgrupadasAsync(mesaId);
		if (!lineas.Any()) return;

		var mesa = await _db.Mesas.FindAsync(mesaId);
		var zonaId = mesa?.ZonaId ?? 0;
		var mesaNombre = mesa?.Nombre ?? $"{mesaId}";
		var subtotal = lineas.Sum(l => l.Cantidad * l.Precio);
		var (lineasConPluses, total) = await AplicarPlusesAsync(lineas, subtotal, zonaId);
		var (cab, pie, desglose, impuestos) = await ObtenerTextoPlantillaAsync();
		var bytes = EscPosService.GenerarProforma(cab, pie, mesaId, mesaNombre, lineasConPluses, total, desglose, impuestos);
		await GuardarTrabajoFacturaAsync(TipoTrabajoPrint.Proforma, bytes, $"Mesa {mesaNombre} – Proforma");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Factura Simplificada
	// ─────────────────────────────────────────────────────────────────────────

	public async Task EnolarFacturaSimpleAsync(int mesaId, MetodoPago metodoPago)
	{
		if (!await _db.Impresoras.AnyAsync(i => i.Activa)) return;
		var lineas = await ObtenerLineasAgrupadasAsync(mesaId);
		if (!lineas.Any()) return;

		var mesa = await _db.Mesas.FindAsync(mesaId);
		var zonaId = mesa?.ZonaId ?? 0;
		var mesaNombre = mesa?.Nombre ?? $"{mesaId}";
		var subtotal = lineas.Sum(l => l.Cantidad * l.Precio);
		var (lineasConPluses, total) = await AplicarPlusesAsync(lineas, subtotal, zonaId);
		var (cab, pie, desglose, impuestos) = await ObtenerTextoPlantillaAsync();
		var bytes = EscPosService.GenerarFacturaSimple(cab, pie, mesaId, mesaNombre, lineasConPluses, total, metodoPago, desglose, impuestos);
		await GuardarTrabajoFacturaAsync(TipoTrabajoPrint.FacturaSimple, bytes, $"Mesa {mesaNombre} – Factura Simple");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Helpers
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Lee la plantilla de ticket de la BD y extrae los textos de cabecera y pie.
	/// Devuelve listas de strings (una por elemento de tipo 'texto').
	/// </summary>
	private async Task<(List<string> Cab, List<string> Pie, bool Desglose, bool Impuestos)> ObtenerTextoPlantillaAsync()
	{
		var plantilla = await _db.TicketPlantillas.FirstOrDefaultAsync();
		if (plantilla is null) return (new(), new(), true, false);

		static List<string> Extraer(string json)
		{
			var result = new List<string>();
			try
			{
				var doc = System.Text.Json.JsonDocument.Parse(json);
				foreach (var el in doc.RootElement.EnumerateArray())
				{
					if (el.TryGetProperty("tipo", out var tipo) && tipo.GetString() == "texto"
						&& el.TryGetProperty("contenido", out var cont))
						result.Add(cont.GetString() ?? "");
				}
			}
			catch { }
			return result;
		}

		return (Extraer(plantilla.CabeceraJson), Extraer(plantilla.PieJson),
			plantilla.ImprimirDesglose, plantilla.ImprimirImpuestos);
	}

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
