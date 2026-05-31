namespace Bar_QR.Models;

/// <summary>
/// Recargo porcentual que se suma al total del ticket.
/// DiasJson almacena un array JSON de enteros (0=Dom … 6=Sáb).
/// </summary>
public class Plus
{
	public int     Id         { get; set; }
	public string  Nombre     { get; set; } = "Plus";
	public decimal Porcentaje { get; set; } = 0;
	/// <summary>JSON array de días de la semana (DayOfWeek). Vacío = todos los días.</summary>
	public string  DiasJson   { get; set; } = "[]";
	public bool    Activo     { get; set; } = true;
}
