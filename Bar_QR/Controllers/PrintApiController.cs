using Bar_QR.Data;
using Bar_QR.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bar_QR.Controllers;

/// <summary>
/// API REST consumida por el PrintAgent local.
/// GET  /api/print/pendientes   → lista de trabajos pendientes
/// POST /api/print/{id}/impreso → marca como impreso
/// POST /api/print/{id}/error   → marca como error
/// </summary>
[ApiController]
[Route("api/print")]
public class PrintApiController : ControllerBase
{
	private readonly AppDbContext _db;
	public PrintApiController(AppDbContext db) => _db = db;

	[HttpGet("pendientes")]
	public async Task<IActionResult> Pendientes()
	{
		var trabajos = await _db.TrabajosPrint
			.Where(t => t.Estado == EstadoTrabajoPrint.Pendiente)
			.OrderBy(t => t.CreadoEn)
			.Select(t => new {
				t.Id,
				t.Tipo,
				t.DestinoRol,
				t.ContenidoBase64,
				t.Referencia
			})
			.ToListAsync();

		return Ok(trabajos);
	}

	[HttpPost("{id:int}/impreso")]
	public async Task<IActionResult> MarcarImpreso(int id)
	{
		var t = await _db.TrabajosPrint.FindAsync(id);
		if (t is null) return NotFound();
		t.Estado     = EstadoTrabajoPrint.Impreso;
		t.ImprestoEn = DateTime.UtcNow;
		await _db.SaveChangesAsync();
		return Ok();
	}

	[HttpPost("{id:int}/error")]
	public async Task<IActionResult> MarcarError(int id)
	{
		var t = await _db.TrabajosPrint.FindAsync(id);
		if (t is null) return NotFound();
		t.Estado = EstadoTrabajoPrint.Error;
		await _db.SaveChangesAsync();
		return Ok();
	}
}
