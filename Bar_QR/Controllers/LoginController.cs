using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Bar_QR.Controllers;

public class LoginController : Controller
{
	public IActionResult Index() => View();

	// Botón para Paco o Luis
	[HttpPost]
	public async Task<IActionResult> AccesoCamarero(string nombre)
	{
		var claims = new List<Claim> {
			new Claim(ClaimTypes.Name, nombre),
			new Claim(ClaimTypes.Role, "Camarero")
		};
		await Loguear(claims);
		return RedirectToAction("Index", "Staff");
	}

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
		await HttpContext.SignInAsync("CookieAuth", principal);
	}

	public async Task<IActionResult> Logout()
	{
		await HttpContext.SignOutAsync("CookieAuth");
		return RedirectToAction("Index", "Carta");
	}
}