using System.Drawing.Printing;
using System.Net.Http.Json;
using System.Runtime.InteropServices;

namespace PrintAgent;

/// <summary>
/// Worker que cada N segundos consulta la API cloud por trabajos pendientes,
/// los imprime en la impresora local correspondiente y confirma la impresión.
/// </summary>
public class PrintWorker : BackgroundService
{
	private readonly IHttpClientFactory _factory;
	private readonly IConfiguration     _cfg;
	private readonly ILogger<PrintWorker> _log;

	// Intervalo de polling (configurable en appsettings.json → "PollingSeconds")
	private TimeSpan Intervalo => TimeSpan.FromSeconds(
		_cfg.GetValue<int?>("PollingSeconds") ?? 5);

	public PrintWorker(IHttpClientFactory factory, IConfiguration cfg, ILogger<PrintWorker> log)
	{
		_factory = factory;
		_cfg     = cfg;
		_log     = log;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_log.LogInformation("PrintAgent iniciado. Polling cada {s}s", Intervalo.TotalSeconds);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await ProcesarTrabajosPendientesAsync(stoppingToken);
			}
			catch (Exception ex)
			{
				_log.LogError(ex, "Error en ciclo de polling");
			}

			await Task.Delay(Intervalo, stoppingToken);
		}
	}

	private async Task ProcesarTrabajosPendientesAsync(CancellationToken ct)
	{
		var http = _factory.CreateClient("cloud");

		// GET /api/print/pendientes  → lista de TrabajoPrintDto
		var trabajos = await http.GetFromJsonAsync<List<TrabajoPrintDto>>(
			"/api/print/pendientes", ct);

		if (trabajos is null || trabajos.Count == 0) return;

		foreach (var t in trabajos)
		{
			try
			{
				var bytes = Convert.FromBase64String(t.ContenidoBase64);
				var impresora = ObtenerNombreImpresora(t.DestinoRol);
				Imprimir(bytes, impresora, t.Referencia);

				// POST /api/print/{id}/impreso  → marca como impreso
				await http.PostAsync($"/api/print/{t.Id}/impreso", null, ct);
				_log.LogInformation("Trabajo {Id} ({Ref}) impreso en '{Imp}'", t.Id, t.Referencia, impresora);
			}
			catch (Exception ex)
			{
				_log.LogWarning(ex, "No se pudo imprimir trabajo {Id}", t.Id);
				// POST /api/print/{id}/error
				await http.PostAsync($"/api/print/{t.Id}/error", null, ct);
			}
		}
	}

	/// <summary>
	/// Devuelve el nombre de impresora Windows configurado para el rol.
	/// Definir en appsettings.json: "Printers": { "Barra": "...", "Cocina": "...", "Todas": "..." }
	/// </summary>
	private string ObtenerNombreImpresora(int rolDestino)
	{
		string seccion = rolDestino switch
		{
			0 => "Barra",
			1 => "Cocina",
			_ => "Todas"
		};
		return _cfg[$"Printers:{seccion}"]
			?? _cfg["Printers:Todas"]
			?? throw new InvalidOperationException(
				$"No hay impresora configurada para el rol '{seccion}'. " +
				"Añádela en appsettings.json → Printers.");
	}

	/// <summary>
	/// Envía los bytes ESC/POS crudos a la impresora Windows usando PrintDocument (GDI+).
	/// Para impresoras USB/COM la forma más directa es escribir directo al puerto;
	/// aquí usamos el método de RawPrinterHelper (P/Invoke) para máxima compatibilidad.
	/// </summary>
	private static void Imprimir(byte[] bytes, string nombreImpresora, string docNombre)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			RawPrinterHelper.EnviarBytesRaw(nombreImpresora, docNombre, bytes);
		}
		else
		{
			// En Linux/macOS: escribir al dispositivo directamente (ej: /dev/usb/lp0)
			File.WriteAllBytes(nombreImpresora, bytes);
		}
	}
}
