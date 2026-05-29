namespace Bar_QR.Models;

public class Empleado
{
	public int Id { get; set; }
	public string Nombre { get; set; } = string.Empty;
	/// <summary>
	/// "avatar_h1".."avatar_h4", "avatar_m1".."avatar_m4" para avatares genéricos,
	/// o "custom" cuando hay foto subida.
	/// </summary>
	public string AvatarTipo { get; set; } = "avatar_h1";
	public byte[]? FotoData { get; set; }
	public string? FotoMime { get; set; }
	/// <summary>PIN numérico opcional para login sin Google (4 dígitos).</summary>
	public string? Pin { get; set; }
	/// <summary>Rol: Camarero o Encargado.</summary>
	public string Rol { get; set; } = "Camarero";
}
