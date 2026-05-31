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

		var zonaLabel = pedido.Mesa?.Zona?.Nombre ?? "";
		int mesa      = pedido.MesaId;

		var barra  = pedido.Lineas.Where(l => l.Producto?.DestinoImpresion == DestinoImpresion.Barra).ToList();
		var cocina = pedido.Lineas.Where(l => l.Producto?.DestinoImpresion == DestinoImpresion.Cocina).ToList();

		if (barra.Any())
		{
			var bytes = EscPosService.GenerarComanda(mesa, zonaLabel, "BARRA",
				barra.Select(l => (l.Producto!.Nombre, l.Cantidad)));
			await GuardarTrabajoAsync(TipoTrabajoPrint.ComandaBarra, RolImpresora.Barra, bytes,
				$"Mesa {mesa} – Pedido #{pedidoId}");
		}

		if (cocina.Any())
		{
			var bytes = EscPosService.GenerarComanda(mesa, zonaLabel, "COCINA",
				cocina.Select(l => (l.Producto!.Nombre, l.Cantidad)));
			await GuardarTrabajoAsync(TipoTrabajoPrint.ComandaCocina, RolImpresora.Cocina, bytes,
				$"Mesa {mesa} – Pedido #{pedidoId}");
		}
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Factura Proforma
	// ─────────────────────────────────────────────────────────────────────────

	public async Task EnolarProformaAsync(int mesaId)
	{
		var lineas = await ObtenerLineasAgrupadasAsync(mesaId);
		if (!lineas.Any()) return;

		var total = lineas.Sum(l => l.Cantidad * l.Precio);
		var bytes = EscPosService.GenerarProforma(_cabecera, mesaId, lineas, total);
		await GuardarTrabajoAsync(TipoTrabajoPrint.Proforma, RolImpresora.Todas, bytes,
			$"Mesa {mesaId} – Proforma");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Factura Simplificada
	// ─────────────────────────────────────────────────────────────────────────

	public async Task EnolarFacturaSimpleAsync(int mesaId, MetodoPago metodoPago)
	{
		var lineas = await ObtenerLineasAgrupadasAsync(mesaId);
		if (!lineas.Any()) return;

		var total = lineas.Sum(l => l.Cantidad * l.Precio);
		var bytes = EscPosService.GenerarFacturaSimple(_cabecera, mesaId, lineas, total, metodoPago);
		await GuardarTrabajoAsync(TipoTrabajoPrint.FacturaSimple, RolImpresora.Todas, bytes,
			$"Mesa {mesaId} – Factura Simple");
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
}
