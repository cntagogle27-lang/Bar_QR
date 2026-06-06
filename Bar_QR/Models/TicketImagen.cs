namespace Bar_QR.Models;

/// <summary>
/// Imagen subida para usar en cabecera/pie del ticket, almacenada en la BD.
/// </summary>
public class TicketImagen
{
	public int    Id          { get; set; }
	public string Nombre      { get; set; } = string.Empty;
	public byte[] Data        { get; set; } = Array.Empty<byte>();
	public string MimeType    { get; set; } = "image/png";
	public string Zona        { get; set; } = "cabecera"; // "cabecera" | "pie"
}
