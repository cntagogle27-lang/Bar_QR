using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bar_QR.Models;
using Microsoft.EntityFrameworkCore;

namespace Bar_QR.Controllers;

[Authorize(Roles = "Camarero,Encargado")]
public class StaffController : Controller
{
	private readonly Bar_QR.Data.AppDbContext _db;

	public StaffController(Bar_QR.Data.AppDbContext db)
	{
		_db = db;
	}

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
		ViewData["ZonaId"] = zonaId;
		ViewData["ZonaNombre"] = zona.Nombre;
		return View(zona.Mesas.OrderBy(m => m.NumeroMesa).ToList());
	}

	// ─── PANEL DE PEDIDO ────────────────────────────────────────────────────────

	public IActionResult Panel(int mesaId, int zonaId, int? pedidoId = null)
	{
		var mesa = _db.Mesas.Find(mesaId);
		if (mesa == null) return RedirectToAction("MapaMesas", new { zonaId });

		PedidoMesa? pedido = null;

		// Si viene pedidoId explícito, cargarlo directamente
		if (pedidoId.HasValue)
			pedido = _db.PedidosMesa
				.Include(p => p.Lineas).ThenInclude(l => l.Producto)
				.FirstOrDefault(p => p.Id == pedidoId.Value && p.MesaId == mesaId);

		// Si no, buscar el más reciente con estado Abierto (0)
		pedido ??= _db.PedidosMesa
			.Include(p => p.Lineas).ThenInclude(l => l.Producto)
			.Where(p => p.MesaId == mesaId && (int)p.Estado == 0)
			.OrderByDescending(p => p.CreadoEn)
			.FirstOrDefault();

		if (pedido == null)
		{
			var nuevo = new PedidoMesa { MesaId = mesaId, CreadoEn = DateTime.UtcNow, Estado = EstadoPedidoMesa.Abierto };
			_db.PedidosMesa.Add(nuevo);
			_db.SaveChanges();
			pedido = _db.PedidosMesa
				.Include(p => p.Lineas).ThenInclude(l => l.Producto)
				.First(p => p.Id == nuevo.Id);
		}

		var productos = _db.Productos.OrderBy(p => (int)p.Grupo).ThenBy(p => p.Nombre).ToList();

		ViewData["ZonaId"]      = zonaId;
		ViewData["MesaNombre"]  = mesa.Nombre;
		ViewData["MesaId"]      = mesaId;
		ViewData["PedidoId"]    = pedido.Id;
		ViewData["EsEncargado"] = User.IsInRole("Encargado") || User.IsInRole("Admin");
		return View((pedido, productos));
	}

	[HttpPost]
	public IActionResult AgregarLinea(int pedidoId, int productoId, int zonaId, int mesaId)
	{
		var pedido = _db.PedidosMesa.Include(p => p.Lineas).FirstOrDefault(p => p.Id == pedidoId);
		if (pedido == null) return RedirectToAction("MapaMesas", new { zonaId });

		var esEncargado = User.IsInRole("Encargado") || User.IsInRole("Admin");
		if (pedido.Estado == EstadoPedidoMesa.Enviado && !esEncargado)
			return RedirectToAction("Panel", new { mesaId, zonaId, pedidoId });

		var linea = pedido.Lineas.FirstOrDefault(l => l.ProductoId == productoId);
		if (linea != null)
			linea.Cantidad++;
		else
			pedido.Lineas.Add(new LineaPedido { PedidoMesaId = pedidoId, ProductoId = productoId, Cantidad = 1 });

		_db.SaveChanges();
		return RedirectToAction("Panel", new { mesaId, zonaId, pedidoId });
	}

	[HttpPost]
	public IActionResult QuitarLinea(int lineaId, int pedidoId, int zonaId, int mesaId)
	{
		var pedido = _db.PedidosMesa.FirstOrDefault(p => p.Id == pedidoId);
		var linea  = _db.LineasPedido.FirstOrDefault(l => l.Id == lineaId && l.PedidoMesaId == pedidoId);

		if (pedido == null || linea == null)
			return RedirectToAction("Panel", new { mesaId, zonaId, pedidoId });

		var esEncargado = User.IsInRole("Encargado") || User.IsInRole("Admin");
		if (pedido.Estado == EstadoPedidoMesa.Enviado && !esEncargado)
			return RedirectToAction("Panel", new { mesaId, zonaId, pedidoId });

		if (linea.Cantidad > 1)
			linea.Cantidad--;
		else
			_db.LineasPedido.Remove(linea);

		_db.SaveChanges();
		return RedirectToAction("Panel", new { mesaId, zonaId, pedidoId });
	}

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
}
