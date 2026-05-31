namespace Bar_QR.Models;

public enum TipoTrabajoPrint { ComandaBarra, ComandaCocina, Proforma, FacturaSimple }
public enum EstadoTrabajoPrint { Pendiente, Impreso, Error }
public enum MetodoPago { Efectivo, Tarjeta, Mixto }

/// <summary>Trabajo de impresión pendiente que el agente local descargará y enviará a la impresora.</summary>
public class TrabajoPrint
{
	public int Id { get; set; }
	public TipoTrabajoPrint Tipo { get; set; }
	public EstadoTrabajoPrint Estado { get; set; } = EstadoTrabajoPrint.Pendiente;
	public RolImpresora DestinoRol { get; set; }   // qué impresora debe procesarlo
	public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
	public DateTime? ImprestoEn { get; set; }
	/// <summary>Bytes ESC/POS en Base64 para transporte JSON.</summary>
	public string ContenidoBase64 { get; set; } = string.Empty;
	/// <summary>Referencia opcional (ej: "Mesa 5 – Pedido #12")</summary>
	public string Referencia { get; set; } = string.Empty;
}
