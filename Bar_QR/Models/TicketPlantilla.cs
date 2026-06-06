namespace Bar_QR.Models;

/// <summary>
/// Plantilla de ticket del local. Solo existe un registro (Id = 1).
/// CabeceraJson y PieJson almacenan listas de elementos (texto/imagen) serializados.
/// </summary>
public class TicketPlantilla
{
	public int Id { get; set; } = 1;

	// Opciones del cuerpo
	public bool ImprimirHora      { get; set; } = true;
	public bool ImprimirUsuario   { get; set; } = true;
	public bool ImprimirImpuestos { get; set; } = false;
	public bool ImprimirDesglose  { get; set; } = true;

	// Elementos de cabecera y pie serializados como JSON
	// Cada elemento: { tipo: "texto"|"imagen", contenido: "...", x, y, w, h }
	public string CabeceraJson { get; set; } = "[]";
	public string PieJson      { get; set; } = "[]";
}
