using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Bar_QR.Controllers;

public class LoginController : Controller
{
	public IActionResult Index() => View();

	[HttpPost]
	public async Task<IActionResult> AccesoCamareroEmail(string email)
	{
		// Comprueba si el email está en la base de datos
		using (var scope = HttpContext.RequestServices.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
			if (db.StaffEmails.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
			{
				var claims = new List<Claim> {
					new Claim(ClaimTypes.Name, email),
					new Claim(ClaimTypes.Role, "Camarero")
				};
				await Loguear(claims);
				return RedirectToAction("Index", "Staff");
			}
		}
		// Si no está, vuelve al login con error simple
		TempData["LoginError"] = "Correo no autorizado";
		return RedirectToAction("Index");
	}

	[HttpGet]
	public async Task<IActionResult> Acceso(string email)
	{
		if (string.IsNullOrWhiteSpace(email))
		{
			return RedirectToAction("Index", "Carta");
		}

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

	// Botón para Admin con PIN
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