using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bar_QR.Models;
using Microsoft.EntityFrameworkCore;

namespace Bar_QR.Controllers;

[Authorize(Roles = "Camarero,Encargado")]
public class StaffController : Controller
{
	private readonly Bar_QR.Data.AppDbContext _db;
	public StaffController(Bar_QR.Data.AppDbContext db) => _db = db;

	public IActionResult Index() => RedirectToAction("Zonas");

	public IActionResult Zonas()
	{
		var zonas = _db.Zonas.Include(z => z.Mesas).OrderBy(z => z.Nombre).ToList();
		return View(zonas);
	}

	public IActionResult MapaMesas(int zonaId)
	{
		var zona = _db.Zonas.Include(z => z.Mesas).FirstOrDefault(z => z.Id == zonaId);
		if (zona == null) return RedirectToAction("Zonas");
		ViewData["ZonaId"]     = zonaId;
		ViewData["ZonaNombre"] = zona.Nombre;
		return View(zona.Mesas.OrderBy(m => m.NumeroMesa).ToList());
	}

	// ─── helpers ────────────────────────────────────────────────────────────────

	private PedidoMesa? ObtenerPedido(int mesaId)
	{
		var id = _db.Database
			.SqlQueryRaw<int>(
				"SELECT Id FROM PedidosMesa WHERE MesaId = {0} ORDER BY Estado ASC, CreadoEn DESC LIMIT 1",
				mesaId)
			.AsEnumerable()
			.FirstOrDefault();

		if (id <= 0) return null;

		return _db.PedidosMesa
			.Include(p => p.Lineas)
			.ThenInclude(l => l.Producto)
			.FirstOrDefault(p => p.Id == id);
	}

	private PedidoMesa ObtenerOCrearPedido(int mesaId)
	{
		var pedido = ObtenerPedido(mesaId);
		if (pedido != null) return pedido;

		var nuevo = new PedidoMesa { MesaId = mesaId, CreadoEn = DateTime.UtcNow, Estado = EstadoPedidoMesa.Abierto };
		_db.PedidosMesa.Add(nuevo);
		_db.SaveChanges();
		return _db.PedidosMesa
			.Include(p => p.Lineas).ThenInclude(l => l.Producto)
			.First(p => p.Id == nuevo.Id);
	}

	// ─── PANEL ──────────────────────────────────────────────────────────────────

	public IActionResult Panel(int mesaId, int zonaId)
	{
		var mesa = _db.Mesas.Find(mesaId);
		if (mesa == null) return RedirectToAction("MapaMesas", new { zonaId });

		var pedido   = ObtenerOCrearPedido(mesaId);
		var productos = _db.Productos.OrderBy(p => (int)p.Grupo).ThenBy(p => p.Nombre).ToList();

		ViewData["ZonaId"]      = zonaId;
		ViewData["MesaNombre"]  = mesa.Nombre;
		ViewData["MesaId"]      = mesaId;
		ViewData["PedidoId"]    = pedido.Id;
		ViewData["EsEncargado"] = User.IsInRole("Encargado") || User.IsInRole("Admin");
		return View((pedido, productos));
	}

	// ─── AGREGAR ────────────────────────────────────────────────────────────────

	[HttpPost]
	public IActionResult AgregarLinea(int pedidoId, int productoId, int zonaId, int mesaId, int cantidad = 1)
	{
		var pedido = _db.PedidosMesa.Include(p => p.Lineas)
			.FirstOrDefault(p => p.Id == pedidoId && p.MesaId == mesaId)
			?? ObtenerPedido(mesaId);

		if (pedido == null) return RedirectToAction("MapaMesas", new { zonaId });

		var esEnc = User.IsInRole("Encargado") || User.IsInRole("Admin");
		if (pedido.Estado == EstadoPedidoMesa.Enviado && !esEnc)
			return RedirectToAction("Panel", new { mesaId, zonaId });

		if (cantidad < 1) cantidad = 1;

		var linea = pedido.Lineas.FirstOrDefault(l => l.ProductoId == productoId);
		if (linea != null)
			linea.Cantidad += cantidad;
		else
			pedido.Lineas.Add(new LineaPedido { PedidoMesaId = pedido.Id, ProductoId = productoId, Cantidad = cantidad });

		_db.SaveChanges();
		return RedirectToAction("Panel", new { mesaId, zonaId });
	}

	// ─── QUITAR ─────────────────────────────────────────────────────────────────

	[HttpPost]
	public IActionResult QuitarLinea(int lineaId, int pedidoId, int zonaId, int mesaId, string? motivo = null)
	{
		var pedido = _db.PedidosMesa.FirstOrDefault(p => p.Id == pedidoId);
		var linea  = _db.LineasPedido.FirstOrDefault(l => l.Id == lineaId && l.PedidoMesaId == pedidoId);

		if (pedido == null || linea == null) return RedirectToAction("Panel", new { mesaId, zonaId });

		var esEnc = User.IsInRole("Encargado") || User.IsInRole("Admin");

		// Pedido enviado: solo encargado puede borrar (motivo requerido)
		if (pedido.Estado == EstadoPedidoMesa.Enviado && !esEnc)
			return RedirectToAction("Panel", new { mesaId, zonaId });

		// Pedido enviado y encargado: registrar motivo antes de borrar (motivo llega del modal)
		if (linea.Cantidad > 1 && pedido.Estado == EstadoPedidoMesa.Abierto)
			linea.Cantidad--;
		else
			_db.LineasPedido.Remove(linea);

		_db.SaveChanges();
		return RedirectToAction("Panel", new { mesaId, zonaId });
	}

	// ─── CAMBIAR PRECIO (encargado) ──────────────────────────────────────────────

	[HttpPost]
	public IActionResult CambiarPrecio(int lineaId, int pedidoId, int zonaId, int mesaId, decimal precio)
	{
		if (!User.IsInRole("Encargado") && !User.IsInRole("Admin"))
			return RedirectToAction("Panel", new { mesaId, zonaId });

		var linea = _db.LineasPedido.FirstOrDefault(l => l.Id == lineaId && l.PedidoMesaId == pedidoId);
		if (linea != null)
		{
			linea.PrecioOverride = precio >= 0 ? precio : null;
			_db.SaveChanges();
		}
		return RedirectToAction("Panel", new { mesaId, zonaId });
	}

	// ─── CAMBIAR CANTIDAD (encargado) ────────────────────────────────────────────

	[HttpPost]
	public IActionResult CambiarCantidad(int lineaId, int pedidoId, int zonaId, int mesaId, int cantidad)
	{
		if (!User.IsInRole("Encargado") && !User.IsInRole("Admin"))
			return RedirectToAction("Panel", new { mesaId, zonaId });

		var linea = _db.LineasPedido.FirstOrDefault(l => l.Id == lineaId && l.PedidoMesaId == pedidoId);
		if (linea != null)
		{
			if (cantidad <= 0)
				_db.LineasPedido.Remove(linea);
			else
				linea.Cantidad = cantidad;
			_db.SaveChanges();
		}
		return RedirectToAction("Panel", new { mesaId, zonaId });
	}

	// ─── ENVIAR ──────────────────────────────────────────────────────────────────

	[HttpPost]
	public IActionResult EnviarPedido(int pedidoId, int zonaId, int mesaId)
	{
		var pedido = _db.PedidosMesa.FirstOrDefault(p => p.Id == pedidoId);
		if (pedido != null && pedido.Estado == EstadoPedidoMesa.Abierto)
		{
			pedido.Estado = EstadoPedidoMesa.Enviado;
			var mesa = _db.Mesas.Find(pedido.MesaId);
			if (mesa != null) mesa.Estado = EstadoMesa.Ocupada;
			_db.SaveChanges();
		}
		return RedirectToAction("MapaMesas", new { zonaId });
	}

	// ─── DIAGNÓSTICO ─────────────────────────────────────────────────────────────
	[AllowAnonymous]
	public IActionResult DiagPedidos(int mesaId = 0)
	{
		var pedidos = mesaId > 0
			? _db.PedidosMesa.Include(p => p.Lineas).ThenInclude(l => l.Producto).Where(p => p.MesaId == mesaId).ToList()
			: _db.PedidosMesa.Include(p => p.Lineas).ThenInclude(l => l.Producto).ToList();

		var sb = new System.Text.StringBuilder();
		sb.AppendLine($"<pre>Pedidos ({pedidos.Count}):\n");
		foreach (var p in pedidos)
		{
			sb.AppendLine($"  PedidoId={p.Id} MesaId={p.MesaId} Estado={p.Estado}({(int)p.Estado}) Lineas={p.Lineas.Count}");
			foreach (var l in p.Lineas)
				sb.AppendLine($"    LineaId={l.Id} Producto={l.Producto?.Nombre} Cant={l.Cantidad} PrecOv={l.PrecioOverride}");
		}
		sb.AppendLine("</pre>");
		return Content(sb.ToString(), "text/html");
	}
}
