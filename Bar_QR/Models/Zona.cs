namespace Bar_QR.Models;

public class Zona
{
	public int Id { get; set; }
	public string Nombre { get; set; } = string.Empty;
	public ICollection<Mesa> Mesas { get; set; } = new List<Mesa>();
}
