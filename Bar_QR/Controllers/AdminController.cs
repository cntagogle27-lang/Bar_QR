using Bar_QR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bar_QR.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using Microsoft.Data.Sqlite;

namespace Bar_QR.Controllers;

[Authorize(Roles = "Admin")] // <--- ESTE ES EL CANDADO
public class AdminController : Controller
{
	// Esta lista simula nuestra base de datos de productos
    public static List<Producto> ListaProductosAdmin = new List<Producto>(); // Lista de productos administrados
    // Lista de correos permitidos para personal (gestión desde Ajustes)
    public static List<string> ListaEmailsStaff = new List<string> { // Correos del personal
        "paco@bar.local",
        "luis@bar.local"
    }; // Fin de la lista de correos

    // Lista de IPs permitidas para detección automática (whitelist)
    public static List<string> ListaIPsStaff = new List<string> { // IPs permitidas
        "127.0.0.1",
        "::1"
    }; // Fin de la lista de IPs
    // Lista de proxies confiables que pueden inyectar X-Forwarded-For
    public static List<string> ListaProxies = new List<string> { // Proxies confiables
        "127.0.0.1"
    }; // Fin de la lista de proxies
    // Tokens para QR del cliente (pueden ser uno por local o por mesa según se genere)
    public static List<string> ListaSiteTokens = new List<string>();

    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult NuevoProducto()
	{
		return View(); // Esto le dice: "busca el archivo NuevoProducto.cshtml"
	}

    [HttpPost]
    public IActionResult Guardar(Producto nuevo)
    {
        _db.Productos.Add(nuevo);
        _db.SaveChanges();
        return RedirectToAction("Listado");
    }

    public IActionResult Listado()
    {
        var productos = _db.Productos.ToList();
        return View(productos);
    }

    public IActionResult Ajustes()
    {
        try
        {
            var vm = new Models.AdminAjustesViewModel
            {
                Emails = _db.StaffEmails.Select(e => e.Email).ToList(),
                Ips = _db.ProxyIps.Select(p => p.IpOrCidr).ToList(),
                Proxies = _db.ProxyIps.Select(p => p.IpOrCidr).ToList(),
                Tokens = _db.SiteTokens.Select(t => t.Token).ToList()
            };
            return View(vm);
        }
        catch
        {
            ViewData["AjustesError"] = "No se pudieron cargar los ajustes desde la base de datos.";
            return View(new Models.AdminAjustesViewModel());
        }
    }

    // Ajustes: gestionar correos del personal (gestión desde BD en el método Ajustes anterior)

    [HttpPost]
    public IActionResult AgregarEmail(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["AjustesError"] = "Debes indicar un correo.";
                return RedirectToAction("Ajustes");
            }

            var normalizedEmail = email.Trim();
            if (!EsEmailValido(normalizedEmail))
            {
                TempData["AjustesError"] = "El formato del correo no es válido.";
                return RedirectToAction("Ajustes");
            }

            if (_db.StaffEmails.Any(e => e.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["AjustesError"] = "Ese correo ya está autorizado.";
                return RedirectToAction("Ajustes");
            }

            _db.StaffEmails.Add(new StaffEmail { Email = normalizedEmail });
            _db.SaveChanges();
            TempData["AjustesOk"] = "Correo añadido correctamente.";
        }
        catch (DbUpdateException ex)
        {
            TempData["AjustesError"] = $"No se pudo añadir el correo: {ex.GetBaseException().Message}";
        }
        catch (SqliteException ex)
        {
            TempData["AjustesError"] = $"No se pudo añadir el correo: {ex.Message}";
        }
        catch
        {
            TempData["AjustesError"] = "No se pudo añadir el correo.";
        }
        return RedirectToAction("Ajustes");
    }

    [HttpPost]
    public IActionResult AgregarToken(string token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            if (!_db.SiteTokens.Any(t => t.Token.Equals(token, StringComparison.OrdinalIgnoreCase)))
            {
                _db.SiteTokens.Add(new SiteToken { Token = token.Trim() });
                _db.SaveChanges();
            }
        }
        return RedirectToAction("Ajustes");
    }

    [HttpPost]
    public IActionResult EliminarToken(string token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            _db.SiteTokens.RemoveRange(_db.SiteTokens.Where(t => t.Token.Equals(token, StringComparison.OrdinalIgnoreCase)));
            _db.SaveChanges();
        }
        return RedirectToAction("Ajustes");
    }

    [HttpPost]
    public IActionResult AgregarProxy(string proxy)
    {
        if (!string.IsNullOrWhiteSpace(proxy))
        {
            if (!_db.ProxyIps.Any(p => p.IpOrCidr.Equals(proxy, StringComparison.OrdinalIgnoreCase)))
            {
                _db.ProxyIps.Add(new ProxyIp { IpOrCidr = proxy.Trim() });
                _db.SaveChanges();
            }
        }
        return RedirectToAction("Ajustes");
    }

    [HttpPost]
    public IActionResult EliminarProxy(string proxy)
    {
        if (!string.IsNullOrWhiteSpace(proxy))
        {
            _db.ProxyIps.RemoveRange(_db.ProxyIps.Where(p => p.IpOrCidr.Equals(proxy, StringComparison.OrdinalIgnoreCase)));
            _db.SaveChanges();
        }
        return RedirectToAction("Ajustes");
    }

    [HttpPost]
    public IActionResult EliminarEmail(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["AjustesError"] = "Debes indicar un correo para eliminar.";
                return RedirectToAction("Ajustes");
            }

            var normalizedEmail = email.Trim();
            var emails = _db.StaffEmails.Where(e => e.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!emails.Any())
            {
                TempData["AjustesError"] = "Ese correo no existe en la lista autorizada.";
                return RedirectToAction("Ajustes");
            }

            _db.StaffEmails.RemoveRange(emails);
            _db.SaveChanges();
            TempData["AjustesOk"] = "Correo eliminado correctamente.";
        }
        catch (DbUpdateException ex)
        {
            TempData["AjustesError"] = $"No se pudo eliminar el correo: {ex.GetBaseException().Message}";
        }
        catch (SqliteException ex)
        {
            TempData["AjustesError"] = $"No se pudo eliminar el correo: {ex.Message}";
        }
        catch
        {
            TempData["AjustesError"] = "No se pudo eliminar el correo.";
        }
        return RedirectToAction("Ajustes");
    }

    private static bool EsEmailValido(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [HttpPost]
    public IActionResult AgregarIP(string ip)
    {
        if (!string.IsNullOrWhiteSpace(ip))
        {
            if (!_db.Mesas.Any(m => false)) { }
            if (!_db.ProxyIps.Any(p => p.IpOrCidr.Equals(ip, StringComparison.OrdinalIgnoreCase)))
            {
                _db.ProxyIps.Add(new ProxyIp { IpOrCidr = ip.Trim() });
                _db.SaveChanges();
            }
        }
        return RedirectToAction("Ajustes");
    }

    [HttpPost]
    public IActionResult EliminarIP(string ip)
    {
        if (!string.IsNullOrWhiteSpace(ip))
        {
            _db.ProxyIps.RemoveRange(_db.ProxyIps.Where(p => p.IpOrCidr.Equals(ip, StringComparison.OrdinalIgnoreCase)));
            _db.SaveChanges();
        }
        return RedirectToAction("Ajustes");
    }
} // <-- Esta cierra la Clase