using System.Text;
using Bar_QR.Models;

namespace Bar_QR.Services;

/// <summary>
/// Genera arrays de bytes ESC/POS crudos para impresoras térmicas de 80 mm
/// (aprox. 48 caracteres por línea a 9×12 pt).
/// </summary>
public static class EscPosService
{
	// ── Comandos ESC/POS ────────────────────────────────────────────────────
	private static readonly byte[] Init           = { 0x1B, 0x40 };           // ESC @
	private static readonly byte[] Bold_On        = { 0x1B, 0x45, 0x01 };
	private static readonly byte[] Bold_Off       = { 0x1B, 0x45, 0x00 };
	private static readonly byte[] Align_Center   = { 0x1B, 0x61, 0x01 };
	private static readonly byte[] Align_Left     = { 0x1B, 0x61, 0x00 };
	private static readonly byte[] Font_Double    = { 0x1D, 0x21, 0x11 };    // ancho×2 + alto×2
	private static readonly byte[] Font_Normal    = { 0x1D, 0x21, 0x00 };
	private static readonly byte[] Cut            = { 0x1D, 0x56, 0x41, 0x03 }; // corte parcial
	private static readonly byte[] LineFeed       = { 0x0A };

	private const int COLS = 48;

	// ── API pública ─────────────────────────────────────────────────────────

	/// <summary>Genera un ticket de comanda (Barra o Cocina).</summary>
	public static byte[] GenerarComanda(
		int numeroMesa,
		string zonaOpcional,
		string destino,                     // "BARRA" | "COCINA"
		IEnumerable<(string Nombre, int Cantidad)> lineas)
	{
		using var ms = new MemoryStream();
		ms.Write(Init);
		ms.Write(Align_Center);
		ms.Write(Bold_On);
		ms.Write(Font_Double);
		ms.WriteLine($"-- {destino} --");
		ms.Write(Font_Normal);
		ms.Write(Bold_Off);
		ms.WriteLine(Separador('-'));
		ms.Write(Align_Left);
		ms.Write(Bold_On);
		ms.WriteLine($"MESA {numeroMesa}  {zonaOpcional}".Trim());
		ms.Write(Bold_Off);
		ms.WriteLine(Separador('-'));
		foreach (var (nombre, cant) in lineas)
			ms.WriteLine(LineaProducto(cant.ToString(), nombre, null));
		ms.Write(LineFeed);
		ms.Write(Cut);
		return ms.ToArray();
	}

	/// <summary>Genera una Factura Proforma.</summary>
	public static byte[] GenerarProforma(
		string cabecera,
		int numeroMesa,
		IEnumerable<(string Nombre, int Cantidad, decimal Precio)> lineas,
		decimal total)
	{
		return GenerarFactura(cabecera, "FACTURA PROFORMA", numeroMesa, lineas, total, null);
	}

	/// <summary>Genera una Factura Simplificada definitiva.</summary>
	public static byte[] GenerarFacturaSimple(
		string cabecera,
		int numeroMesa,
		IEnumerable<(string Nombre, int Cantidad, decimal Precio)> lineas,
		decimal total,
		MetodoPago metodoPago)
	{
		return GenerarFactura(cabecera, "FACTURA SIMPLE", numeroMesa, lineas, total, metodoPago);
	}

	// ── Internos ─────────────────────────────────────────────────────────────

	private static byte[] GenerarFactura(
		string cabecera,
		string tipoLabel,
		int numeroMesa,
		IEnumerable<(string Nombre, int Cantidad, decimal Precio)> lineas,
		decimal total,
		MetodoPago? metodoPago)
	{
		using var ms = new MemoryStream();
		ms.Write(Init);
		ms.Write(Align_Center);
		ms.Write(Bold_On);
		foreach (var linCab in cabecera.Split('\n'))
			ms.WriteLine(Centrar(linCab));
		ms.Write(Bold_Off);
		ms.WriteLine(Separador('='));
		ms.WriteLine(Centrar(tipoLabel));
		ms.WriteLine(Separador('='));
		ms.Write(Align_Left);
		ms.WriteLine($"Mesa: {numeroMesa}");
		ms.WriteLine($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}");
		ms.WriteLine(Separador('-'));
		ms.Write(Bold_On);
		ms.WriteLine(LineaProducto("Cant", "Descripción", "Precio"));
		ms.Write(Bold_Off);
		ms.WriteLine(Separador('-'));
		foreach (var (nombre, cant, precio) in lineas)
			ms.WriteLine(LineaProducto(cant.ToString(), nombre, $"{precio:0.00}€"));
		ms.WriteLine(Separador('='));
		ms.Write(Bold_On);
		ms.WriteLine(LineaTotal("TOTAL", $"{total:0.00}€"));
		ms.Write(Bold_Off);
		if (metodoPago.HasValue)
		{
			ms.WriteLine(Separador('-'));
			ms.WriteLine($"Pago: {MetodoPagoLabel(metodoPago.Value)}");
		}
		ms.Write(LineFeed);
		ms.Write(LineFeed);
		ms.Write(Align_Center);
		ms.WriteLine("¡Gracias por su visita!");
		ms.Write(LineFeed);
		ms.Write(Cut);
		return ms.ToArray();
	}

	// ── Helpers de formato ───────────────────────────────────────────────────

	private static string Separador(char c) => new string(c, COLS);

	private static string Centrar(string texto) =>
		texto.Length >= COLS ? texto[..COLS] : texto.PadLeft((COLS + texto.Length) / 2).PadRight(COLS);

	private static string LineaProducto(string cant, string nombre, string? precio)
	{
		int priceCols = precio is null ? 0 : precio.Length + 1;
		int cantCols  = cant.Length + 1;
		int nombreCols = COLS - cantCols - priceCols;
		string nomTrunc = nombre.Length > nombreCols ? nombre[..nombreCols] : nombre.PadRight(nombreCols);
		return precio is null
			? $"{cant,-4}{nomTrunc}"
			: $"{cant,-4}{nomTrunc}{precio.PadLeft(priceCols)}";
	}

	private static string LineaTotal(string etiqueta, string valor)
	{
		int valorCols = valor.Length;
		int etCols    = COLS - valorCols;
		return $"{etiqueta.PadRight(etCols)}{valor}";
	}

	private static string MetodoPagoLabel(MetodoPago m) => m switch
	{
		MetodoPago.Efectivo => "Efectivo",
		MetodoPago.Tarjeta  => "Tarjeta",
		MetodoPago.Mixto    => "Efectivo + Tarjeta",
		_ => m.ToString()
	};
}

// ── Extensión para MemoryStream ──────────────────────────────────────────────
file static class MemoryStreamExtensions
{
	private static readonly Encoding Enc = Encoding.GetEncoding("ISO-8859-1");

	public static void Write(this MemoryStream ms, byte[] bytes)   => ms.Write(bytes, 0, bytes.Length);
	public static void WriteLine(this MemoryStream ms, string text) => ms.Write(Enc.GetBytes(text + "\n"));
}
