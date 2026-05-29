using System.Diagnostics;
using Bar_QR.Models;
using Microsoft.AspNetCore.Mvc;

namespace Bar_QR.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var email = User.Identity.Name?.Trim();
                if (string.Equals(email, "barqrgm@gmail.com", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Listado", "Admin");
                }

                return RedirectToAction("Index", "Carta");
            }

            // No autenticado: mostrar landing de cliente
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult EscanearQR()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
