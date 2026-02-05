using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bar_QR.Models;

namespace Bar_QR.Controllers;

[Authorize(Roles = "Camarero")] // Solo camareros
public class StaffController : Controller
{
    // Simulamos un mapa de mesas
    private static List<Mesa> Mesas = Enumerable.Range(1, 12).Select(n => new Mesa { Numero = n }).ToList();
    private readonly Bar_QR.Data.AppDbContext _db;

    public StaffController(Bar_QR.Data.AppDbContext db)
    {
        _db = db;
        // ensure mesas in db
        if (!_db.Mesas.Any())
        {
            for (int i = 1; i <= 12; i++) _db.Mesas.Add(new Mesa { Numero = i, Ocupada = false });
            _db.SaveChanges();
        }
    }

    // Método público para que la parte cliente pueda obtener las mesas (usa DB)
    public static List<Mesa> GetMesasPublic()
    {
        // Leer desde el contexto requiere crear un scope; para simplicidad devolvemos la estática si existe
        if (Mesas != null && Mesas.Any()) return Mesas;
        return Enumerable.Range(1,12).Select(n => new Mesa { Numero = n }).ToList();
    }

    public IActionResult Index()
    {
        var dbMesas = _db.Mesas.OrderBy(m => m.Numero).ToList();
        return View(dbMesas);
    }

    [HttpPost]
    public IActionResult Toggle(int numero)
    {
        var m = _db.Mesas.FirstOrDefault(x => x.Numero == numero);
        if (m != null)
        {
            m.Ocupada = !m.Ocupada;
            _db.SaveChanges();
        }
        return RedirectToAction("Index");
    }
}
