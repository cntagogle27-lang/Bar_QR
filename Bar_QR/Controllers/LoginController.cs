using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

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
		var redirectUrl = Url.Action("GoogleCallback", "Login", values: null, protocol: "https");
		Console.WriteLine($"[GoogleLogin] RedirectUri = {redirectUrl}");
		var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
		return Challenge(properties, "Google");
	}

	[HttpGet]
	public async Task<IActionResult> GoogleCallback()
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
		using var scope = HttpContext.RequestServices.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
		if (db.StaffEmails.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
		{
			await LoguearCamarero(email);
			return RedirectToAction("Index", "Staff");
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

		using var scope = HttpContext.RequestServices.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
		var camareros = db.StaffEmails.Select(e => e.Email).ToList();
		ViewData["GoogleEmail"] = email;
		return View(camareros);
	}

	[HttpPost]
	public async Task<IActionResult> SeleccionarPerfil(string emailCamarero)
	{
		if (TempData["GoogleAdminEmail"] is not string adminEmail)
			return RedirectToAction("Index");

		if (string.IsNullOrWhiteSpace(emailCamarero))
		{
			// Admin entra como administrador
			var adminClaims = new List<Claim> {
				new Claim(ClaimTypes.Name, adminEmail),
				new Claim(ClaimTypes.Role, "Admin")
			};
			await Loguear(adminClaims);
			return RedirectToAction("Listado", "Admin");
		}

		using var scope = HttpContext.RequestServices.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
		if (!db.StaffEmails.Any(e => e.Email.Equals(emailCamarero, StringComparison.OrdinalIgnoreCase)))
		{
			TempData["LoginError"] = "Perfil no válido.";
			TempData["GoogleAdminEmail"] = adminEmail;
			return RedirectToAction("SeleccionarPerfil");
		}

		await LoguearCamarero(emailCamarero);
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

	public async Task<IActionResult> Logout()
	{
		await HttpContext.SignOutAsync("CookieAuth");
		return RedirectToAction("Index", "Carta");
	}
}