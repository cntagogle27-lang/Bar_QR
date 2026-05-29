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
		return View();
	}

	[HttpPost]
	public async Task<IActionResult> Guardar(Producto nuevo, IFormFile? foto)
	{
		if (!ModelState.IsValid)
			return View("NuevoProducto", nuevo);

		if (foto != null && foto.Length > 0)
		{
			using var ms = new MemoryStream();
			await foto.CopyToAsync(ms);
			nuevo.FotoData = ms.ToArray();
			nuevo.FotoMimeType = foto.ContentType;
		}
		_db.Productos.Add(nuevo);
		await _db.SaveChangesAsync();
		return RedirectToAction("Listado");
	}

	public IActionResult Listado()
	{
		try
		{
			var productos = _db.Productos.OrderBy(p => p.Grupo).ThenBy(p => p.Nombre).ToList();
			return View(productos);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[Admin.Listado] Error: {ex.Message}");
			return View(new List<Producto>());
		}
	}

	public IActionResult EditarProducto(int id)
	{
		var producto = _db.Productos.Find(id);
		if (producto == null) return NotFound();
		return View(producto);
	}

	[HttpPost]
	public async Task<IActionResult> EditarProducto(Producto editado, IFormFile? foto)
	{
		var producto = _db.Productos.Find(editado.Id);
		if (producto == null) return NotFound();

		producto.Nombre = editado.Nombre;
		producto.Precio = editado.Precio;
		producto.Grupo = editado.Grupo;
		producto.DestinoImpresion = editado.DestinoImpresion;

		if (foto != null && foto.Length > 0)
		{
			using var ms = new MemoryStream();
			await foto.CopyToAsync(ms);
			producto.FotoData = ms.ToArray();
			producto.FotoMimeType = foto.ContentType;
		}

		_db.SaveChanges();
		TempData["ProductoOk"] = "Producto actualizado correctamente.";
		return RedirectToAction("Listado");
	}

	/// <summary>Sirve la foto de un producto directamente desde la base de datos.</summary>
	[AllowAnonymous]
	public IActionResult FotoProducto(int id)
	{
		var producto = _db.Productos.Find(id);
		if (producto?.FotoData == null || producto.FotoMimeType == null)
			return NotFound();
		return File(producto.FotoData, producto.FotoMimeType);
	}

	[HttpPost]
	public IActionResult BorrarProducto(int id)
	{
		var producto = _db.Productos.Find(id);
		if (producto != null)
		{
			_db.Productos.Remove(producto);
			_db.SaveChanges();
			TempData["ProductoOk"] = "Producto eliminado.";
		}
		return RedirectToAction("Listado");
	}

	// ── TICKET PLANTILLA ────────────────────────────────────────────
    [HttpPost]
    public IActionResult GuardarTicketPlantilla(
        bool imprimirHora, bool imprimirUsuario, bool imprimirImpuestos, bool imprimirDesglose,
        string cabeceraJson, string pieJson)
    {
        var plantilla = _db.TicketPlantillas.FirstOrDefault();
        if (plantilla == null)
        {
            plantilla = new Models.TicketPlantilla();
            _db.TicketPlantillas.Add(plantilla);
        }
        plantilla.ImprimirHora      = imprimirHora;
        plantilla.ImprimirUsuario   = imprimirUsuario;
        plantilla.ImprimirImpuestos = imprimirImpuestos;
        plantilla.ImprimirDesglose  = imprimirDesglose;
        plantilla.CabeceraJson      = string.IsNullOrWhiteSpace(cabeceraJson) ? "[]" : cabeceraJson;
        plantilla.PieJson           = string.IsNullOrWhiteSpace(pieJson)      ? "[]" : pieJson;
        _db.SaveChanges();
        TempData["AjustesOk"] = "Plantilla de ticket guardada correctamente.";
        return RedirectToAction("Ajustes", null, "tickets");
    }

    [HttpPost]
    public async Task<IActionResult> SubirImagenTicket(IFormFile imagen, string zona)
    {
        if (imagen == null || imagen.Length == 0)
            return BadRequest();
        using var ms = new MemoryStream();
        await imagen.CopyToAsync(ms);
        var ti = new Models.TicketImagen
        {
            Nombre   = Path.GetFileNameWithoutExtension(imagen.FileName),
            Data     = ms.ToArray(),
            MimeType = imagen.ContentType,
            Zona     = zona ?? "cabecera"
        };
        _db.TicketImagenes.Add(ti);
        _db.SaveChanges();
        return Json(new { id = ti.Id, nombre = ti.Nombre });
    }

    [AllowAnonymous]
    public IActionResult ImagenTicket(int id)
    {
        var img = _db.TicketImagenes.Find(id);
        if (img == null) return NotFound();
        return File(img.Data, img.MimeType);
    }

    [HttpPost]
    public IActionResult EliminarImagenTicket(int id)
    {
        var img = _db.TicketImagenes.Find(id);
        if (img != null) { _db.TicketImagenes.Remove(img); _db.SaveChanges(); }
        return RedirectToAction("Ajustes", null, "tickets");
    }


    public IActionResult Ajustes()
    {
        try
        {
            var vm = new Models.AdminAjustesViewModel
            {
                Emails   = _db.StaffEmails.Select(e => e.Email).ToList(),
                Ips      = _db.ProxyIps.Select(p => p.IpOrCidr).ToList(),
                Proxies  = _db.ProxyIps.Select(p => p.IpOrCidr).ToList(),
                Tokens   = _db.SiteTokens.Select(t => t.Token).ToList(),
                Ticket   = _db.TicketPlantillas.FirstOrDefault() ?? new Models.TicketPlantilla(),
                TicketImagenes = _db.TicketImagenes.Select(i => new Models.TicketImagen { Id = i.Id, Nombre = i.Nombre, MimeType = i.MimeType, Zona = i.Zona, Data = new byte[0] }).ToList(),
                Empleados = _db.Empleados.OrderBy(e => e.Nombre).ToList(),
                Zonas     = _db.Zonas.Include(z => z.Mesas).OrderBy(z => z.Nombre).ToList(),
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

    // ---- MAPA DE MESAS ----

    public IActionResult MapaMesas()
    {
        var mesas = _db.Mesas.OrderBy(m => m.NumeroMesa).ToList();
        return View(mesas);
    }

    [HttpPost]
    public IActionResult EditarMesa(int id, string nombre, int numero)
    {
        var mesa = _db.Mesas.Find(id);
        if (mesa == null) { TempData["MesasError"] = "Mesa no encontrada."; return RedirectToAction("MapaMesas"); }

        var nombreLimpio = string.IsNullOrWhiteSpace(nombre) ? $"Mesa {numero}" : nombre.Trim();
        var slug = GenerarSlug(nombreLimpio, numero);

        // Verificar slug único (excluyendo la propia mesa)
        if (_db.Mesas.Any(m => m.Slug == slug && m.Id != id))
            slug = $"{slug}-{id}";

        mesa.NumeroMesa = numero;
        mesa.Nombre = nombreLimpio;
        mesa.Slug = slug;
        _db.SaveChanges();
        TempData["MesasOk"] = $"Mesa actualizada: {nombreLimpio}";
        return RedirectToAction("MapaMesas");
    }

    [HttpPost]
    public IActionResult AgregarMesa()
    {
        var maxNum = _db.Mesas.Any() ? _db.Mesas.Max(m => m.NumeroMesa) + 1 : 1;
        _db.Mesas.Add(new Mesa { NumeroMesa = maxNum, Nombre = $"Mesa {maxNum}", Slug = $"mesa-{maxNum}", Estado = EstadoMesa.Libre });
        _db.SaveChanges();
        TempData["MesasOk"] = $"Mesa {maxNum} añadida.";
        return RedirectToAction("MapaMesas");
    }

    [HttpPost]
    public IActionResult EliminarMesa(int id)
    {
        var mesa = _db.Mesas.Find(id);
        if (mesa != null) { _db.Mesas.Remove(mesa); _db.SaveChanges(); TempData["MesasOk"] = "Mesa eliminada."; }
        return RedirectToAction("MapaMesas");
    }

    [HttpPost]
    public IActionResult ToggleHabilitarMesa(int id, int? returnZona)
    {
        var mesa = _db.Mesas.Find(id);
        if (mesa != null)
        {
            mesa.Habilitada = !mesa.Habilitada;
            // Al deshabilitar, cerrar sesiones activas de esa mesa
            if (!mesa.Habilitada)
            {
                var sesiones = _db.SesionesMesa.Where(s => s.MesaId == id);
                _db.SesionesMesa.RemoveRange(sesiones);
            }
            _db.SaveChanges();
            TempData["ControlOk"] = mesa.Habilitada ? $"Mesa habilitada." : "Mesa deshabilitada.";
        }
        if (returnZona.HasValue)
            return RedirectToAction("ControlMapaZona", new { zonaId = returnZona.Value });
        return RedirectToAction("MapaMesas");
    }

    private static string GenerarSlug(string nombre, int numero)
    {
        var s = nombre.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("á","a").Replace("é","e").Replace("í","i").Replace("ó","o").Replace("ú","u")
            .Replace("ñ","n");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9\-]", "");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"-+", "-").Trim('-');
        return string.IsNullOrEmpty(s) ? $"mesa-{numero}" : s;
    }

    // ─── CONTROL MESAS ──────────────────────────────────────────────────────────

    public IActionResult ControlMesas()
    {
        var zonas = _db.Zonas.Include(z => z.Mesas).OrderBy(z => z.Nombre).ToList();
        return View(zonas);
    }

    [Route("Admin/ControlMapaZona/{zonaId:int}")]
    public IActionResult ControlMapaZona(int zonaId)
    {
        var zona = _db.Zonas.Include(z => z.Mesas).FirstOrDefault(z => z.Id == zonaId);
        if (zona == null) return NotFound();
        return View(zona);
    }

    [HttpPost]
    public IActionResult ToggleHabilitarZona(int id)
    {
        var zona = _db.Zonas.Find(id);
        if (zona != null)
        {
            zona.Habilitada = !zona.Habilitada;
            _db.SaveChanges();
            TempData["ControlOk"] = zona.Habilitada ? $"Zona «{zona.Nombre}» habilitada." : $"Zona «{zona.Nombre}» deshabilitada.";
        }
        return RedirectToAction("ControlMesas");
    }

    // ─── EMPLEADOS ──────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> AgregarEmpleado(string nombre, string avatarTipo, IFormFile? foto, string? pin, string? rol)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            TempData["AjustesError"] = "El nombre del empleado es obligatorio.";
            return RedirectToAction("Ajustes", null, "empleados");
        }
        var emp = new Empleado { Nombre = nombre.Trim(), AvatarTipo = avatarTipo ?? "avatar_h1", Pin = string.IsNullOrWhiteSpace(pin) ? null : pin.Trim(), Rol = string.IsNullOrWhiteSpace(rol) ? "Camarero" : rol.Trim() };
        if (foto != null && foto.Length > 0)
        {
            using var ms = new MemoryStream();
            await foto.CopyToAsync(ms);
            emp.FotoData = ms.ToArray();
            emp.FotoMime = foto.ContentType;
            emp.AvatarTipo = "custom";
        }
        _db.Empleados.Add(emp);
        _db.SaveChanges();
        return RedirectToAction("Ajustes", null, "empleados");
    }

    [HttpPost]
    public async Task<IActionResult> EditarEmpleado(int id, string nombre, string avatarTipo, IFormFile? foto, string? pin, string? rol)
    {
        var emp = _db.Empleados.Find(id);
        if (emp == null) return NotFound();
        emp.Nombre = string.IsNullOrWhiteSpace(nombre) ? emp.Nombre : nombre.Trim();
        emp.Pin = string.IsNullOrWhiteSpace(pin) ? null : pin.Trim();
        emp.Rol = string.IsNullOrWhiteSpace(rol) ? emp.Rol : rol.Trim();
        if (foto != null && foto.Length > 0)
        {
            using var ms = new MemoryStream();
            await foto.CopyToAsync(ms);
            emp.FotoData = ms.ToArray();
            emp.FotoMime = foto.ContentType;
            emp.AvatarTipo = "custom";
        }
        else
        {
            emp.AvatarTipo = avatarTipo ?? emp.AvatarTipo;
        }
        _db.SaveChanges();
        return RedirectToAction("Ajustes", null, "empleados");
    }

    [HttpPost]
    public IActionResult EliminarEmpleado(int id)
    {
        var emp = _db.Empleados.Find(id);
        if (emp != null) { _db.Empleados.Remove(emp); _db.SaveChanges(); }
        return RedirectToAction("Ajustes", null, "empleados");
    }

    /// <summary>Devuelve todas las zonas con sus mesas para la pestaña QR de Ajustes.</summary>
    public IActionResult QrMesas()
    {
        var zonas = _db.Zonas.Include(z => z.Mesas).OrderBy(z => z.Nombre).ToList();
        ViewData["BaseUrl"] = $"{Request.Scheme}://{Request.Host}";
        return View(zonas);
    }

    [AllowAnonymous]
    public IActionResult FotoEmpleado(int id)
    {
        var emp = _db.Empleados.Find(id);
        if (emp?.FotoData == null) return NotFound();
        return File(emp.FotoData, emp.FotoMime ?? "image/jpeg");
    }

    // ─── ZONAS ──────────────────────────────────────────────────────────────────

    [HttpPost]
    public IActionResult AgregarZona(string nombre)
    {
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            _db.Zonas.Add(new Zona { Nombre = nombre.Trim() });
            _db.SaveChanges();
        }
        return RedirectToAction("Ajustes", null, "mesas");
    }

    [HttpPost]
    public IActionResult RenombrarZona(int id, string nombre)
    {
        var zona = _db.Zonas.Find(id);
        if (zona != null && !string.IsNullOrWhiteSpace(nombre))
        {
            zona.Nombre = nombre.Trim();
            _db.SaveChanges();
        }
        return RedirectToAction("Ajustes", null, "mesas");
    }

    [HttpPost]
    public IActionResult EliminarZona(int id)
    {
        var zona = _db.Zonas.Include(z => z.Mesas).FirstOrDefault(z => z.Id == id);
        if (zona != null)
        {
            foreach (var m in zona.Mesas) m.ZonaId = null;
            _db.Zonas.Remove(zona);
            _db.SaveChanges();
        }
        return RedirectToAction("Ajustes", null, "mesas");
    }

    // ─── MAPA DE ZONA ────────────────────────────────────────────────────────────

    public IActionResult MapaZona(int id)
    {
        var zona = _db.Zonas.Include(z => z.Mesas).FirstOrDefault(z => z.Id == id);
        if (zona == null) return NotFound();
        return View(zona);
    }

    [HttpPost]
    public IActionResult AgregarMesaZona(int zonaId)
    {
        var maxNum = _db.Mesas.Any() ? _db.Mesas.Max(m => m.NumeroMesa) + 1 : 1;
        _db.Mesas.Add(new Mesa
        {
            NumeroMesa = maxNum,
            Nombre = $"Mesa {maxNum}",
            Slug = $"mesa-{maxNum}",
            Estado = EstadoMesa.Libre,
            ZonaId = zonaId,
            PosX = 20, PosY = 20, Ancho = 100, Alto = 80
        });
        _db.SaveChanges();
        return RedirectToAction("MapaZona", new { id = zonaId });
    }

    [HttpPost]
    public IActionResult GuardarPosicionMesa(int id, int posX, int posY, int ancho, int alto, string? nombre)
    {
        var mesa = _db.Mesas.Find(id);
        if (mesa == null) return NotFound();
        mesa.PosX = posX; mesa.PosY = posY;
        mesa.Ancho = ancho; mesa.Alto = alto;
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            mesa.Nombre = nombre.Trim();
            mesa.Slug = GenerarSlug(nombre.Trim(), mesa.NumeroMesa);
        }
        _db.SaveChanges();
        return Ok();
    }

    [HttpPost]
    public IActionResult EliminarMesaZona(int id, int zonaId)
    {
        var mesa = _db.Mesas.Find(id);
        if (mesa != null) { _db.Mesas.Remove(mesa); _db.SaveChanges(); }
        return RedirectToAction("MapaZona", new { id = zonaId });
    }
}
