using Microsoft.AspNetCore.Mvc;
using Bar_QR.Models;

namespace Bar_QR.Controllers;

public class PedidoController : Controller
{
	// Esta lista guardará los productos mientras la web esté abierta
	private static List<Producto> carrito = new List<Producto>();

	public IActionResult Index()
	{
		return View(carrito);
	}

	[HttpPost]
	public IActionResult Agregar(string nombre, string precio)
	{
		// Convertimos el precio que viene de la web a número
		decimal precioDecimal = decimal.Parse(precio, System.Globalization.CultureInfo.InvariantCulture);

		carrito.Add(new Producto { Nombre = nombre, Precio = precioDecimal });

		return Json(new { success = true, mensaje = nombre + " añadido" });
	} // <-- ESTA LLAVE CIERRA 'AGREGAR'

	[HttpPost]
	public IActionResult Confirmar()
	{
		// Vaciamos la lista para que el cliente pueda pedir de nuevo
		carrito.Clear();

		return Json(new { success = true, mensaje = "Pedido enviado y carrito vaciado" });
	}
}