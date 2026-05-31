namespace PrintAgent;

/// <summary>DTO devuelto por la API cloud para cada trabajo pendiente.</summary>
public class TrabajoPrintDto
{
	public int    Id              { get; set; }
	public int    Tipo            { get; set; }   // 0=ComandaBarra 1=ComandaCocina 2=Proforma 3=FacturaSimple
	public int    DestinoRol      { get; set; }   // 0=Barra 1=Cocina 2=Todas
	public string ContenidoBase64 { get; set; } = "";
	public string Referencia      { get; set; } = "";
}
