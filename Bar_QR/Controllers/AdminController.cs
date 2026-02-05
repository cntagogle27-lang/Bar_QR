using Bar_QR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bar_QR.Data;
using Microsoft.EntityFrameworkCore;

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
        var vm = new Models.AdminAjustesViewModel
        {
            Emails = _db.StaffEmails.Select(e => e.Email).ToList(),
            Ips = _db.ProxyIps.Select(p => p.IpOrCidr).ToList(),
            Proxies = _db.ProxyIps.Select(p => p.IpOrCidr).ToList(),
            Tokens = _db.SiteTokens.Select(t => t.Token).ToList()
        };
        return View(vm);
    }

    // Ajustes: gestionar correos del personal
    public IActionResult Ajustes()
    {
        var vm = new Models.AdminAjustesViewModel {
            Emails = ListaEmailsStaff.ToList(),
            Ips = ListaIPsStaff.ToList()
        };
        vm.Proxies = ListaProxies.ToList();
        vm.Tokens = ListaSiteTokens.ToList();
        return View(vm);
    }

    [HttpPost]
    public IActionResult AgregarEmail(string email)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            if (!_db.StaffEmails.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                _db.StaffEmails.Add(new StaffEmail { Email = email.Trim() });
                _db.SaveChanges();
            }
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
        if (!string.IsNullOrWhiteSpace(email))
        {
            _db.StaffEmails.RemoveRange(_db.StaffEmails.Where(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
            _db.SaveChanges();
        }
        return RedirectToAction("Ajustes");
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