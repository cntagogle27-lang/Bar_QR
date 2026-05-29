using Microsoft.AspNetCore.Mvc;
using Bar_QR.Models;
using Bar_QR.Data;
using Microsoft.EntityFrameworkCore;

namespace Bar_QR.Controllers;

public class CartaController : Controller
{
	private const string CookieSesionMesa = "sesion_mesa";
	private readonly AppDbContext _db;

	public CartaController(AppDbContext db)
	{
		_db = db;
	}

	// Página inicial: acceso público para ver la carta (sin mesa).
	// Si viene con token válido de QR, muestra la mesa correspondiente.
	public IActionResult Index(string? token, int? mesa)
	{
		// Solo intentamos validar token si viene uno (acceso por QR)
		if (!string.IsNullOrEmpty(token) && AdminController.ListaSiteTokens.Any(t => t.Equals(token, StringComparison.OrdinalIgnoreCase)))
		{
			if (mesa.HasValue)
				ViewData["MesaSeleccionada"] = mesa.Value;
		}

		var productosReales = _db.Productos.OrderBy(p => (int)p.Grupo).ThenBy(p => p.Nombre).ToList();
		return View(productosReales);
	}

	public IActionResult SeleccionMesa(string token)
	{
		ViewData["Token"] = token;
		var mesas = _db.Mesas.OrderBy(m => m.NumeroMesa).ToList();
		return View(mesas);
	}

	// Este método servirá para cuando el cliente le dé a "Pedir"
	[HttpPost]
	public IActionResult EnviarPedido(List<Producto> carrito, int numMesa)
	{
		return Json(new { success = true, message = "Pedido enviado a cocina" });
	}

	// Acceso por URL única de mesa (escaneo de QR)
	[Route("Carta/Mesa/{slug}")]
	public IActionResult Mesa(string slug)
	{
		// 1. Buscar la mesa
		var mesa = _db.Mesas.FirstOrDefault(m => m.Slug == slug);
		if (mesa == null) return NotFound("Mesa no encontrada.");

		// 2. Validar que la mesa está habilitada (conectada)
		if (!mesa.Habilitada)
			return View("AccesoDenegado", new AccesoDenegadoViewModel
			{
				Motivo = "Esta mesa no está disponible en este momento."
			});

		// 3. Limpiar sesiones expiradas de esta mesa
		var ahora = DateTime.UtcNow;
		var expiradas = _db.SesionesMesa.Where(s => s.MesaId == mesa.Id && s.Expira <= ahora);
		_db.SesionesMesa.RemoveRange(expiradas);
		_db.SaveChanges();

		// 4. Comprobar si hay una sesión activa (otra persona ya está usando esta mesa)
		var tokenCookie = Request.Cookies[CookieSesionMesa];
		var sesionActiva = _db.SesionesMesa
			.FirstOrDefault(s => s.MesaId == mesa.Id && s.Expira > ahora);

		if (sesionActiva != null)
		{
			// Permitir acceso solo si el token de cookie coincide con la sesión activa
			if (sesionActiva.Token != tokenCookie)
			{
				return View("AccesoDenegado", new AccesoDenegadoViewModel
				{
					Motivo = "Esta mesa ya tiene una sesión activa. Vuelve a escanear cuando esté libre."
				});
			}
			// El usuario ya tiene sesión válida, dejar pasar
		}
		else
		{
			// 5. No hay sesión activa: crear una nueva con token dinámico (válida 4h)
			var nuevoToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
			var sesion = new SesionMesa
			{
				MesaId = mesa.Id,
				Token = nuevoToken,
				Expira = ahora.AddHours(4)
			};
			_db.SesionesMesa.Add(sesion);
			_db.SaveChanges();

			// Guardar token en cookie del navegador del cliente (4h, HttpOnly)
			Response.Cookies.Append(CookieSesionMesa, nuevoToken, new CookieOptions
			{
				HttpOnly = true,
				Secure = true,
				SameSite = SameSiteMode.Lax,
				Expires = DateTimeOffset.UtcNow.AddHours(4)
			});
			tokenCookie = nuevoToken;
		}

		var productos = _db.Productos.OrderBy(p => (int)p.Grupo).ThenBy(p => p.Nombre).ToList();
		ViewData["MesaSeleccionada"] = mesa.NumeroMesa;
		ViewData["MesaNombre"] = mesa.Nombre;
		ViewData["SesionToken"] = tokenCookie;
		return View("Index", productos);
	}
}