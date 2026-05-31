namespace Bar_QR.Models;

public class Plus
{
	public int     Id         { get; set; }
	public int?    ZonaId     { get; set; }
	public Zona?   Zona       { get; set; }
	public string  Nombre     { get; set; } = "Plus";
	public decimal Porcentaje { get; set; } = 0;
	/// <summary>JSON array de días de la semana (DayOfWeek). Vacío = todos los días.</summary>
	public string  DiasJson   { get; set; } = "[]";
	public bool    Activo     { get; set; } = true;
}
