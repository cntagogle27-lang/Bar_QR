using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Bar_QR.Models;

namespace Bar_QR.Controllers;

public class LoginController : Controller
{
	// Correo del administrador principal que accede a la intranet/camareros
	private const string AdminEmail = "barqrgm@gmail.com";

	public IActionResult Index() => View();

	// --- GOOGLE OAUTH ---

	[HttpGet]
	public IActionResult GoogleLogin()
	{
		// RedirectUri apunta a AfterGoogle, que es DISTINTO del CallbackPath del middleware (/Login/GoogleCallback)
		var redirectUrl = Url.Action("AfterGoogle", "Login", values: null, protocol: "https");
		Console.WriteLine($"[GoogleLogin] RedirectUri = {redirectUrl}");
		var properties = new AuthenticationProperties
		{
			RedirectUri = redirectUrl,
			IsPersistent = false
		};
		return Challenge(properties, "Google");
	}

	[HttpGet]
	public IActionResult GoogleCallback() => Redirect("/"); // solo por si acaso; el middleware intercepta esta ruta

	[HttpGet]
	public async Task<IActionResult> AfterGoogle()
	{
		// Leer el ticket del esquema externo temporal
		var result = await HttpContext.AuthenticateAsync("External");
		if (!result.Succeeded)
		{
			var errorMsg = result.Failure?.Message ?? "No se pudo autenticar con Google.";
			TempData["LoginError"] = $"Error Google: {errorMsg}";
			return RedirectToAction("Index");
		}

		var email = result.Principal?.FindFirstValue(ClaimTypes.Email)
					?? result.Principal?.FindFirstValue("email")
					?? result.Principal?.Identity?.Name;

		if (string.IsNullOrEmpty(email))
		{
			TempData["LoginError"] = "No se pudo obtener el correo de Google.";
			return RedirectToAction("Index");
		}

		// Limpiar cookie externa temporal
		await HttpContext.SignOutAsync("External");

		// Admin principal → seleccionar perfil de camarero
		if (string.Equals(email, AdminEmail, StringComparison.OrdinalIgnoreCase))
		{
			TempData["GoogleAdminEmail"] = email;
			return RedirectToAction("SeleccionarPerfil");
		}

		// Otros correos autorizados como camarero
		try
		{
			using var scope = HttpContext.RequestServices.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
			if (db.StaffEmails.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
			{
				await LoguearCamarero(email);
				return RedirectToAction("Index", "Staff");
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[AfterGoogle] Error al consultar StaffEmails: {ex.Message}");
		}

		TempData["LoginError"] = $"El correo {email} no está autorizado.";
		return RedirectToAction("Index");
	}

	[HttpGet]
	public IActionResult SeleccionarPerfil()
	{
		// Solo accesible si viene del flujo Google
		if (TempData["GoogleAdminEmail"] is not string email)
			return RedirectToAction("Index");

		// Mantener el email disponible para el POST
		TempData.Keep("GoogleAdminEmail");

		try
		{
			using var scope = HttpContext.RequestServices.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
			var empleados = db.Empleados.OrderBy(e => e.Nombre).ToList();
			ViewData["GoogleEmail"] = email;
			return View(empleados);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[SeleccionarPerfil] Error BD: {ex.Message}");
			ViewData["GoogleEmail"] = email;
			return View(new List<Empleado>());
		}
	}

	private const string AdminPin = "1234";

	[HttpPost]
	public async Task<IActionResult> SeleccionarPerfil(int? empleadoId, string? adminPin, string? pinEmpleado)
	{
		if (TempData["GoogleAdminEmail"] is not string adminEmail)
			return RedirectToAction("Index");

		if (empleadoId == null)
		{
			// Verificar PIN de admin
			if (adminPin != AdminPin)
			{
				TempData["LoginError"] = "PIN incorrecto.";
				TempData["GoogleAdminEmail"] = adminEmail;
				return RedirectToAction("SeleccionarPerfil");
			}
			var adminClaims = new List<Claim> {
				new Claim(ClaimTypes.Name, adminEmail),
				new Claim(ClaimTypes.Role, "Admin")
			};
			await Loguear(adminClaims);
			return RedirectToAction("Listado", "Admin");
		}

		using var scope = HttpContext.RequestServices.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
		var empleado = db.Empleados.Find(empleadoId.Value);
		if (empleado == null)
		{
			TempData["LoginError"] = "Perfil no válido.";
			TempData["GoogleAdminEmail"] = adminEmail;
			return RedirectToAction("SeleccionarPerfil");
		}

		// Si el empleado tiene PIN, verificarlo
		if (!string.IsNullOrWhiteSpace(empleado.Pin))
		{
			if (string.IsNullOrWhiteSpace(pinEmpleado) || pinEmpleado.Trim() != empleado.Pin.Trim())
			{
				TempData["LoginError"] = $"PIN incorrecto para {empleado.Nombre}.";
				TempData["GoogleAdminEmail"] = adminEmail;
				return RedirectToAction("SeleccionarPerfil");
			}
		}

		var rol = string.IsNullOrWhiteSpace(empleado.Rol) ? "Camarero" : empleado.Rol.Trim();
		var empClaims = new List<Claim> {
			new Claim(ClaimTypes.Name, empleado.Nombre),
			new Claim(ClaimTypes.Role, rol)
		};
		await Loguear(empClaims);
		return RedirectToAction("Index", "Staff");
	}

	// --- ACCESO DIRECTO POR ENLACE ÚNICO (sin Google) ---

	[HttpGet]
	public async Task<IActionResult> Acceso(string email)
	{
		if (string.IsNullOrWhiteSpace(email))
			return RedirectToAction("Index", "Carta");

		using var scope = HttpContext.RequestServices.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
		if (db.StaffEmails.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
		{
			await LoguearCamarero(email.Trim());
			return RedirectToAction("Index", "Staff");
		}

		TempData["LoginError"] = "Correo no autorizado";
		return RedirectToAction("Index");
	}

	// --- ADMIN POR PIN ---

	[HttpPost]
	public async Task<IActionResult> VerificarAdmin(string pin)
	{
		if (pin == "1234")
		{
			var claims = new List<Claim> {
				new Claim(ClaimTypes.Name, "Administrador"),
				new Claim(ClaimTypes.Role, "Admin")
			};
			await Loguear(claims);
			return RedirectToAction("Listado", "Admin");
		}
		return RedirectToAction("Index");
	}

	// --- HELPERS ---

	private async Task LoguearCamarero(string email)
	{
		var claims = new List<Claim> {
			new Claim(ClaimTypes.Name, email),
			new Claim(ClaimTypes.Role, "Camarero")
		};
		await Loguear(claims);
	}

	private async Task Loguear(List<Claim> claims)
	{
		var identity = new ClaimsIdentity(claims, "CookieAuth");
		var principal = new ClaimsPrincipal(identity);
		var properties = new AuthenticationProperties
		{
			IsPersistent = true,
			ExpiresUtc = DateTimeOffset.UtcNow.AddDays(365)
		};
		await HttpContext.SignInAsync("CookieAuth", principal, properties);
	}

	[Microsoft.AspNetCore.Authorization.AllowAnonymous]
	public IActionResult AvatarEmpleado(int id)
	{
		using var scope = HttpContext.RequestServices.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
		var emp = db.Empleados.Find(id);
		if (emp?.FotoData == null || emp.FotoMime == null) return NotFound();
		return File(emp.FotoData, emp.FotoMime);
	}

	public async Task<IActionResult> Logout()
	{
		await HttpContext.SignOutAsync("CookieAuth");
		return RedirectToAction("Index", "Login");
	}
}