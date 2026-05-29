namespace Bar_QR.Models;

public class Zona
{
	public int Id { get; set; }
	public string Nombre { get; set; } = string.Empty;
	/// <summary>Si false, todas las mesas de la zona quedan deshabilitadas para los clientes.</summary>
	public bool Habilitada { get; set; } = true;
	public ICollection<Mesa> Mesas { get; set; } = new List<Mesa>();
}
