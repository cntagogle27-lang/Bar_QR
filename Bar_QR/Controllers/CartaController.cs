using Microsoft.AspNetCore.Mvc;
using Bar_QR.Models;

namespace Bar_QR.Controllers;

public class CartaController : Controller
{
    private readonly Bar_QR.Data.AppDbContext _db;

    public CartaController(Bar_QR.Data.AppDbContext db)
    {
        _db = db;
    }
    // Página inicial: requiere token válido; si no, redirige a selector de mesa
    public IActionResult Index(string token, int? mesa)
    {
        // Si no tenemos token válido, redirigimos al selector
        if (string.IsNullOrEmpty(token) || !AdminController.ListaSiteTokens.Any(t => t.Equals(token, StringComparison.OrdinalIgnoreCase)))
        {
            return RedirectToAction("SeleccionMesa", new { token });
        }

        // Guardamos la mesa en ViewData para mostrarla discretamente
        if (mesa.HasValue)
        {
            ViewData["MesaSeleccionada"] = mesa.Value;
        }

        var productosReales = _db.Productos.OrderBy(p => (int)p.Grupo).ThenBy(p => p.Nombre).ToList();
        return View(productosReales);
    }

    public IActionResult SeleccionMesa(string token)
    {
        // Mostramos selector de mesa antes de llevar al cliente a la carta
        ViewData["Token"] = token;
        var mesas = _db.Mesas.OrderBy(m => m.NumeroMesa).ToList();
        return View(mesas);
    }

	// Este método servirá para cuando el cliente le dé a "Pedir"
	[HttpPost]
	public IActionResult EnviarPedido(List<Producto> carrito, int numMesa)
	{
		// Aquí irá la lógica para que el pedido llegue a la cocina/barra
		// Por ahora, solo simulamos que se ha enviado
		return Json(new { success = true, message = "Pedido enviado a cocina" });
	}

	// Acceso por URL única de mesa (escaneo de QR)
	[Route("Carta/Mesa/{slug}")]
	public IActionResult Mesa(string slug)
	{
		var mesa = _db.Mesas.FirstOrDefault(m => m.Slug == slug);
		if (mesa == null) return NotFound("Mesa no encontrada.");
		var productos = _db.Productos.OrderBy(p => (int)p.Grupo).ThenBy(p => p.Nombre).ToList();
		ViewData["MesaSeleccionada"] = mesa.NumeroMesa;
		ViewData["MesaNombre"] = mesa.Nombre;
		return View("Index", productos);
	}
}