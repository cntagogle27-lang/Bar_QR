namespace Bar_QR.Models;

/// <summary>
/// Grupos de carta. El orden del enum define el orden de aparición en la vista cliente.
/// 1-CafeInfusiones 2-Desayunos 3-Vinos 4-Bebidas 5-Ensaladas 6-Entrantes
/// 7-Quesos 8-Horno 9-Carnes 10-Postres 11-LicoresCocteles
/// </summary>
public enum GrupoProducto
{
	CafeInfusiones    = 1,
	Desayunos         = 2,
	Vinos             = 3,
	Bebidas           = 4,
	Ensaladas         = 5,
	Entrantes         = 6,
	Quesos            = 7,
	Horno             = 8,
	Carnes            = 9,
	Postres           = 10,
	LicoresCocteles   = 11
}

public enum DestinoImpresion
{
	Barra,
	Cocina
}

public class Producto
{
	public int Id { get; set; }
	public string Nombre { get; set; } = string.Empty;
	public decimal Precio { get; set; }
	public GrupoProducto Grupo { get; set; }
	public DestinoImpresion DestinoImpresion { get; set; }
	/// <summary>Ruta relativa desde wwwroot, ej: /uploads/productos/foto.jpg</summary>
	public string? FotoUrl { get; set; }
	/// <summary>Bytes de la imagen guardada en base de datos (persiste en Railway)</summary>
	public byte[]? FotoData { get; set; }
	/// <summary>MIME type de la imagen, ej: image/webp, image/jpeg</summary>
	public string? FotoMimeType { get; set; }
}