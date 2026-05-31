namespace Bar_QR.Models;

public enum EstadoPedidoMesa
{
	Abierto,   // el camarero está añadiendo productos
	Enviado    // ya enviado a cocina/barra; solo el encargado puede modificarlo
}

/// <summary>Pedido activo de una mesa (uno por mesa en un momento dado).</summary>
public class PedidoMesa
{
	public int Id { get; set; }
	public int MesaId { get; set; }
	public Mesa? Mesa { get; set; }
	public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
	public EstadoPedidoMesa Estado { get; set; } = EstadoPedidoMesa.Abierto;
	public ICollection<LineaPedido> Lineas { get; set; } = new List<LineaPedido>();
}

/// <summary>Línea de un pedido: producto + cantidad.</summary>
public class LineaPedido
{
	public int Id { get; set; }
	public int PedidoMesaId { get; set; }
	public PedidoMesa? Pedido { get; set; }
	public int ProductoId { get; set; }
	public Producto? Producto { get; set; }
	public int Cantidad { get; set; } = 1;
	/// <summary>Precio puntual modificado por el encargado (null = usar Producto.Precio)</summary>
	public decimal? PrecioOverride { get; set; }
}
