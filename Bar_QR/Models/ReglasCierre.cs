namespace Bar_QR.Models;

/// <summary>
/// Regla de cierre automático de mesas.
/// En los días y franja horaria indicados, todas las mesas quedan deshabilitadas.
/// DiasJson almacena un array JSON de enteros (0=Dom … 6=Sáb).
/// HoraInicio / HoraFin en formato "HH:mm".
/// </summary>
public class ReglasCierre
{
	public int    Id         { get; set; }
	public string Nombre     { get; set; } = "Cierre";
	/// <summary>JSON array de días (DayOfWeek). Vacío = todos los días.</summary>
	public string DiasJson   { get; set; } = "[]";
	public string HoraInicio { get; set; } = "00:00";
	public string HoraFin    { get; set; } = "08:00";
	public bool   Activa     { get; set; } = true;
}
