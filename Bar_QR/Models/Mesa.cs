namespace Bar_QR.Models;

public enum EstadoMesa
{
	Libre,
	Ocupada,
	Reservada,
	EsperandoPedido,
	PendientePago
}

public class Mesa
{
	public int Id { get; set; }
	public int NumeroMesa { get; set; }
	public string Nombre { get; set; } = string.Empty;
	public string Slug { get; set; } = string.Empty;
	public EstadoMesa Estado { get; set; } = EstadoMesa.Libre;
	/// <summary>Si false, el cliente no puede acceder aunque escanee el QR.</summary>
	public bool Habilitada { get; set; } = true;

	// Zona a la que pertenece esta mesa
	public int? ZonaId { get; set; }
	public Zona? Zona { get; set; }

	// Posición y tamaño en el mapa 2D
	public int PosX { get; set; } = 20;
	public int PosY { get; set; } = 20;
	public int Ancho { get; set; } = 100;
	public int Alto { get; set; } = 80;
}

/// <summary>Sesión activa de un cliente en una mesa (token dinámico, expira en 4h).</summary>
public class SesionMesa
{
	public int Id { get; set; }
	public int MesaId { get; set; }
	public string Token { get; set; } = string.Empty;
	public DateTime Expira { get; set; }
	public Mesa? Mesa { get; set; }
}