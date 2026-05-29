using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bar_QR.Models;
using Microsoft.EntityFrameworkCore;

namespace Bar_QR.Controllers;

[Authorize(Roles = "Camarero")]
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

	[HttpPost]
	public IActionResult Toggle(int mesaId, int zonaId)
	{
		var m = _db.Mesas.Find(mesaId);
		if (m != null)
		{
			m.Estado = m.Estado == EstadoMesa.Libre ? EstadoMesa.Ocupada : EstadoMesa.Libre;
			_db.SaveChanges();
		}
		return RedirectToAction("MapaMesas", new { zonaId });
	}
}
