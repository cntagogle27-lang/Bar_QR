namespace Bar_QR.Models;

public enum CategoriaProducto
{
	Bebida,
	Comida,
	Postre
}

public enum DestinoImpresion
{
	Barra,
	Cocina
}

public class Producto
{
	public int Id { get; set; }
	public string Nombre { get; set; }
	public decimal Precio { get; set; }
	public CategoriaProducto Categoria { get; set; }
	public DestinoImpresion DestinoImpresion { get; set; }
}