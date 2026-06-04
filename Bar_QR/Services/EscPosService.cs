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
		IEnumerable<string> cabeceraLineas,
		IEnumerable<string> pieLineas,
		int numeroMesa,
		string mesaNombre,
		IEnumerable<(string Nombre, int Cantidad, decimal Precio)> lineas,
		decimal total,
		bool imprimirDesglose = true,
		bool imprimirImpuestos = false)
	{
		return GenerarFactura(cabeceraLineas, pieLineas, "FACTURA PROFORMA", numeroMesa, mesaNombre, lineas, total, null, imprimirDesglose, imprimirImpuestos);
	}

	/// <summary>Genera una Factura Simplificada definitiva.</summary>
	public static byte[] GenerarFacturaSimple(
		IEnumerable<string> cabeceraLineas,
		IEnumerable<string> pieLineas,
		int numeroMesa,
		string mesaNombre,
		IEnumerable<(string Nombre, int Cantidad, decimal Precio)> lineas,
		decimal total,
		MetodoPago metodoPago,
		bool imprimirDesglose = true,
		bool imprimirImpuestos = false)
	{
		return GenerarFactura(cabeceraLineas, pieLineas, "FACTURA SIMPLE", numeroMesa, mesaNombre, lineas, total, metodoPago, imprimirDesglose, imprimirImpuestos);
	}

	// ── Internos ─────────────────────────────────────────────────────────────

	private static byte[] GenerarFactura(
		IEnumerable<string> cabeceraLineas,
		IEnumerable<string> pieLineas,
		string tipoLabel,
		int numeroMesa,
		string mesaNombre,
		IEnumerable<(string Nombre, int Cantidad, decimal Precio)> lineas,
		decimal total,
		MetodoPago? metodoPago,
		bool imprimirDesglose = true,
		bool imprimirImpuestos = false)
	{
		using var ms = new MemoryStream();
		ms.Write(Init);
		ms.Write(Codepage1252);
		ms.Write(Align_Center);
		ms.Write(Bold_On);
		foreach (var linCab in cabeceraLineas)
			ms.WriteLine(Centrar(linCab));
		ms.Write(Bold_Off);
		ms.WriteLine(Separador('='));
		ms.WriteLine(Centrar(tipoLabel));
		ms.WriteLine(Separador('='));
		ms.Write(Align_Left);
		ms.WriteLine($"Mesa: {mesaNombre}");
		ms.WriteLine($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}");
		ms.WriteLine(Separador('-'));
		var lineasList = lineas.ToList();
		if (imprimirDesglose)
		{
			ms.Write(Bold_On);
			ms.WriteLine(LineaProducto("Uds.", "Descripción", "Importe"));
			ms.Write(Bold_Off);
			ms.WriteLine(Separador('-'));
			foreach (var (nombre, cant, precio) in lineasList)
				ms.WriteLine(LineaProducto(cant.ToString(), nombre, $"{precio:0.00}€"));
			ms.WriteLine(Separador('='));
		}
		else
		{
			ms.WriteLine(Separador('='));
		}
		ms.Write(Bold_On);
		ms.WriteLine(LineaTotal("TOTAL", $"{total:0.00}€"));
		ms.Write(Bold_Off);
		if (imprimirImpuestos && total > 0)
		{
			const decimal iva = 0.10m;
			var baseImp = Math.Round(total / (1 + iva), 2);
			var cuota   = Math.Round(total - baseImp, 2);
			ms.WriteLine(Separador('-'));
			ms.WriteLine($"10% Base: {baseImp,8:0.00}€  Cuota: {cuota,6:0.00}€");
			ms.WriteLine($"Total (Imp. Incl.)       {total,8:0.00}€");
		}
		if (metodoPago.HasValue)
		{
			ms.WriteLine(Separador('-'));
			ms.WriteLine($"Pago: {MetodoPagoLabel(metodoPago.Value)}");
		}
		// Pie de la plantilla
		var pieList = pieLineas.ToList();
		if (pieList.Any())
		{
			ms.WriteLine(Separador('-'));
			ms.Write(Align_Center);
			foreach (var linPie in pieList)
				ms.WriteLine(Centrar(linPie));
		}
		ms.Write(LineFeed);
		ms.Write(LineFeed);
		ms.Write(Cut);
		return ms.ToArray();
	}

	// ── Helpers de formato ───────────────────────────────────────────────────

	private static string Separador(char c) => new string(c, COLS);

	private static string Centrar(string texto, int cols = COLS) =>
		texto.Length >= cols ? texto[..cols] : texto.PadLeft((cols + texto.Length) / 2).PadRight(cols);

	private const int CANT_W = 5; // "Uds." + 1 espacio → 5 chars

	private static string LineaProducto(string cant, string nombre, string? precio)
	{
		int priceCols  = precio is null ? 0 : precio.Length + 1;
		int nombreCols = COLS - CANT_W - priceCols;
		if (nombreCols < 1) nombreCols = 1;
		string cantPad  = cant.PadRight(CANT_W);
		string nomTrunc = nombre.Length > nombreCols ? nombre[..nombreCols] : nombre.PadRight(nombreCols);
		return precio is null
			? $"{cantPad}{nomTrunc}"
			: $"{cantPad}{nomTrunc}{precio.PadLeft(priceCols)}";
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
