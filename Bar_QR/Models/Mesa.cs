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
}