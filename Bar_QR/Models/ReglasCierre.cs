namespace Bar_QR.Models;

public class ReglasCierre
{
	public int    Id         { get; set; }
	public int    ZonaId     { get; set; }
	public Zona?  Zona       { get; set; }
	public string Nombre     { get; set; } = "Cierre";
	/// <summary>JSON array de días (DayOfWeek). Vacío = todos los días.</summary>
	public string DiasJson   { get; set; } = "[]";
	public string HoraInicio { get; set; } = "00:00";
	public string HoraFin    { get; set; } = "08:00";
	public bool   Activa     { get; set; } = true;
}
