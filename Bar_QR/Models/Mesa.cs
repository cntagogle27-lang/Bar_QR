namespace Bar_QR.Models;

public enum EstadoMesa
{
	Libre,
	Ocupada,
	Reservada,
	EsperandoPedido
}

public class Mesa
{
	public int Id { get; set; }
	public int NumeroMesa { get; set; }
	public string Nombre { get; set; } = string.Empty;
	public string Slug { get; set; } = string.Empty;
	public EstadoMesa Estado { get; set; } = EstadoMesa.Libre;

	// Zona a la que pertenece esta mesa
	public int? ZonaId { get; set; }
	public Zona? Zona { get; set; }

	// Posición y tamaño en el mapa 2D
	public int PosX { get; set; } = 20;
	public int PosY { get; set; } = 20;
	public int Ancho { get; set; } = 100;
	public int Alto { get; set; } = 80;
}