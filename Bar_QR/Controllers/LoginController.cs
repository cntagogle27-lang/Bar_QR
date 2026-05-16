using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

namespace Bar_QR.Controllers;

public class LoginController : Controller
{
	public IActionResult Index() => View();

	// --- GOOGLE OAUTH ---

	[HttpGet]
	public IActionResult GoogleLogin()
	{
		var props = new AuthenticationProperties { RedirectUri = Url.Action("GoogleCallback") };
		return Challenge(props, GoogleDefaults.AuthenticationScheme);
	}

	[HttpGet]
	public async Task<IActionResult> GoogleCallback()
	{
		var result = await HttpContext.AuthenticateAsync("CookieAuth");
		if (!result.Succeeded) { TempData["LoginError"] = "Error al autenticar con Google."; return RedirectToAction("Index"); }

		var email = result.Principal?.FindFirstValue(ClaimTypes.Email)
				 ?? result.Principal?.FindFirstValue("email")
				 ?? "";

		// Admin
		if (string.Equals(email, "barqrgm@gmail.com", StringComparison.OrdinalIgnoreCase))
		{
			await HttpContext.SignOutAsync("CookieAuth");
			var claims = new List<Claim> {
				new Claim(ClaimTypes.Name, email),
				new Claim(ClaimTypes.Role, "Admin")
			};
			await Loguear(claims);
			return RedirectToAction("Listado", "Admin");
		}

		// Camarero autorizado → selector de perfil
		using (var scope = HttpContext.RequestServices.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
			if (db.StaffEmails.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
			{
				// Guardamos el email verificado en sesión temporal y mostramos perfiles
				TempData["GoogleEmail"] = email;
				await HttpContext.SignOutAsync("CookieAuth"); // limpiamos la cookie provisional de Google
				return RedirectToAction("SeleccionarPerfil");
			}
		}

		await HttpContext.SignOutAsync("CookieAuth");
		TempData["LoginError"] = $"El correo {email} no está autorizado.";
		return RedirectToAction("Index");
	}

	[HttpGet]
	public IActionResult SeleccionarPerfil()
	{
		// Necesita venir de GoogleCallback (email en TempData)
		if (TempData.Peek("GoogleEmail") is not string email)
			return RedirectToAction("Index");

		using var scope = HttpContext.RequestServices.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
		var emails = db.StaffEmails.Select(e => e.Email).ToList();
		ViewData["GoogleEmail"] = email;
		return View(emails);
	}

	[HttpPost]
	public async Task<IActionResult> ConfirmarPerfil(string email, string googleEmail)
	{
		// Verificar que el perfil elegido coincide con el email de Google autenticado
		if (!string.Equals(email, googleEmail, StringComparison.OrdinalIgnoreCase))
		{
			TempData["LoginError"] = "No puedes seleccionar un perfil que no es el tuyo.";
			return RedirectToAction("Index");
		}

		using var scope = HttpContext.RequestServices.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
		if (!db.StaffEmails.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
		{
			TempData["LoginError"] = "Perfil no autorizado.";
			return RedirectToAction("Index");
		}

		var claims = new List<Claim> {
			new Claim(ClaimTypes.Name, email),
			new Claim(ClaimTypes.Role, "Camarero")
		};
		await Loguear(claims);
		return RedirectToAction("Index", "Staff");
	}

	// --- ACCESO POR ENLACE ÚNICO (sigue funcionando) ---

	[HttpGet]
	public async Task<IActionResult> Acceso(string email)
	{
		if (string.IsNullOrWhiteSpace(email))
			return RedirectToAction("Index", "Carta");

		using (var scope = HttpContext.RequestServices.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
			if (db.StaffEmails.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
			{
				var claims = new List<Claim> {
					new Claim(ClaimTypes.Name, email.Trim()),
					new Claim(ClaimTypes.Role, "Camarero")
				};
				await Loguear(claims);
				return RedirectToAction("Index", "Staff");
			}
		}

		TempData["LoginError"] = "Correo no autorizado";
		return RedirectToAction("Index");
	}

	// --- ADMIN CON PIN (acceso secundario) ---

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
		TempData["LoginError"] = "PIN incorrecto.";
		return RedirectToAction("Index");
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