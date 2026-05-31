namespace Bar_QR.Models;

public enum RolImpresora { Barra, Cocina, Todas }

public class Impresora
{
	public int Id { get; set; }
	public string Nombre { get; set; } = string.Empty;
	/// <summary>Puerto USB (ej: USB001), COM (ej: COM3) o IP:puerto (ej: 192.168.1.100:9100)</summary>
	public string Direccion { get; set; } = string.Empty;
	public RolImpresora Rol { get; set; } = RolImpresora.Todas;
	public bool Activa { get; set; } = true;
	/// <summary>Si true, esta impresora también recibe Facturas Proforma y Simples (además de las comandas de su rol).</summary>
	public bool ImprimeFacturas { get; set; } = false;
}
