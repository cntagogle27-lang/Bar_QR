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
	private static readonly byte[] Codepage1252   = { 0x1B, 0x74, 0x10 };    // ESC t 16 → WPC1252 (ó,á,é,í,ú,ñ,¡,¿...)
	private static readonly byte[] Bold_On        = { 0x1B, 0x45, 0x01 };
	private static readonly byte[] Bold_Off       = { 0x1B, 0x45, 0x00 };
	private static readonly byte[] Align_Center   = { 0x1B, 0x61, 0x01 };
	private static readonly byte[] Align_Left     = { 0x1B, 0x61, 0x00 };
	private static readonly byte[] Font_Double    = { 0x1D, 0x21, 0x11 };    // ancho×2 + alto×2
	private static readonly byte[] Font_Normal    = { 0x1D, 0x21, 0x00 };
	private static readonly byte[] Cut            = { 0x1D, 0x56, 0x41, 0x03 }; // corte parcial
	private static readonly byte[] LineFeed       = { 0x0A };

	private const int COLS        = 48;
	private const int COLS_DOUBLE = 24; // con Font_Double cada carácter ocupa 2 columnas

	// ── API pública ─────────────────────────────────────────────────────────

	/// <summary>Genera un ticket de solicitud de cuenta por el cliente.</summary>
	public static byte[] GenerarSolicitudCuenta(string zonaNombre, string mesaNombre, int numeroMesa)
	{
		using var ms = new MemoryStream();
		ms.Write(Init);
		ms.Write(Codepage1252);
		ms.Write(Align_Center);
		ms.Write(Bold_On);
		ms.Write(Font_Double);
		ms.WriteLine(Centrar("CUENTA", COLS_DOUBLE));
		ms.Write(Font_Normal);
		ms.Write(Bold_Off);
		ms.WriteLine(Separador('='));
		ms.Write(Align_Left);
		ms.Write(Bold_On);
		ms.WriteLine($"Cuenta Mesa: {mesaNombre}");
		ms.Write(Bold_Off);
		ms.WriteLine(Separador('='));
		ms.Write(LineFeed);
		ms.Write(LineFeed);
		ms.Write(Cut);
		return ms.ToArray();
	}

	/// <summary>Genera un ticket de comanda (Barra o Cocina).</summary>
	public static byte[] GenerarComanda(
		int numeroMesa,
		string mesaNombre,
		string zonaOpcional,
		string destino,                     // "BARRA" | "COCINA"
		IEnumerable<(string Nombre, int Cantidad)> lineas)
	{
		using var ms = new MemoryStream();
		ms.Write(Init);
		ms.Write(Codepage1252);
		ms.Write(Align_Center);
		ms.Write(Bold_On);
		ms.Write(Font_Double);
		ms.WriteLine(Centrar($"-- {destino} --", COLS_DOUBLE));
		ms.Write(Font_Normal);
		ms.Write(Bold_Off);
		ms.WriteLine(Separador('-'));
		ms.Write(Align_Left);
		ms.Write(Bold_On);
		ms.WriteLine($"M: {mesaNombre}");
		if (!string.IsNullOrWhiteSpace(zonaOpcional))
			ms.WriteLine($"Salon: {zonaOpcional}");
		ms.Write(Bold_Off);
		ms.WriteLine(Separador('-'));
		foreach (var (nombre, cant) in lineas)
		{
			ms.Write(Bold_On);
			ms.WriteLine($"M: {mesaNombre} : {cant}x {nombre}");
			ms.Write(Bold_Off);
		}
		ms.Write(LineFeed);
		ms.Write(Cut);
		return ms.ToArray();
	}

	/// <summary>Genera una Factura Proforma.</summary>
	public static byte[] GenerarProforma(
		string cabecera,
		int numeroMesa,
		string mesaNombre,
		IEnumerable<(string Nombre, int Cantidad, decimal Precio)> lineas,
		decimal total)
	{
		return GenerarFactura(cabecera, "FACTURA PROFORMA", numeroMesa, mesaNombre, lineas, total, null);
	}

	/// <summary>Genera una Factura Simplificada definitiva.</summary>
	public static byte[] GenerarFacturaSimple(
		string cabecera,
		int numeroMesa,
		string mesaNombre,
		IEnumerable<(string Nombre, int Cantidad, decimal Precio)> lineas,
		decimal total,
		MetodoPago metodoPago)
	{
		return GenerarFactura(cabecera, "FACTURA SIMPLE", numeroMesa, mesaNombre, lineas, total, metodoPago);
	}

	// ── Internos ─────────────────────────────────────────────────────────────

	private static byte[] GenerarFactura(
		string cabecera,
		string tipoLabel,
		int numeroMesa,
		string mesaNombre,
		IEnumerable<(string Nombre, int Cantidad, decimal Precio)> lineas,
		decimal total,
		MetodoPago? metodoPago)
	{
		using var ms = new MemoryStream();
		ms.Write(Init);
		ms.Write(Codepage1252);
		ms.Write(Align_Center);
		ms.Write(Bold_On);
		foreach (var linCab in cabecera.Split('\n'))
			ms.WriteLine(Centrar(linCab));
		ms.Write(Bold_Off);
		ms.WriteLine(Separador('='));
		ms.WriteLine(Centrar(tipoLabel));
		ms.WriteLine(Separador('='));
		ms.Write(Align_Left);
		ms.WriteLine($"Mesa: {mesaNombre}");
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

	private static string Centrar(string texto, int cols = COLS) =>
		texto.Length >= cols ? texto[..cols] : texto.PadLeft((cols + texto.Length) / 2).PadRight(cols);

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
	// Windows-1252 coincide con ESC t 16; en Linux requiere CodePagesEncodingProvider registrado
	private static readonly Encoding Enc = GetEnc();
	private static Encoding GetEnc()
	{
		try { return Encoding.GetEncoding(1252); }
		catch { return Encoding.Latin1; } // fallback si el sistema no lo tiene
	}

	public static void Write(this MemoryStream ms, byte[] bytes)   => ms.Write(bytes, 0, bytes.Length);
	public static void WriteLine(this MemoryStream ms, string text) => ms.Write(Enc.GetBytes(text + "\n"));
}
