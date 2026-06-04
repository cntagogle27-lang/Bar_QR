using System.Net.Http.Json;
using System.Runtime.InteropServices;

namespace PrintAgent;

/// <summary>
/// Worker que cada N segundos consulta la API cloud por trabajos pendientes,
/// los imprime en la impresora local correspondiente y confirma la impresión.
/// Las impresoras se leen desde la app (GET /api/print/impresoras) en cada ciclo.
/// </summary>
public class PrintWorker : BackgroundService
{
	private readonly IHttpClientFactory _factory;
	private readonly IConfiguration     _cfg;
	private readonly ILogger<PrintWorker> _log;

	// Caché de impresoras obtenidas de la app
	private List<ImpresoraDto> _impresoras = new();

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
				await RefrescarImpresorasAsync(stoppingToken);
				await ProcesarTrabajosPendientesAsync(stoppingToken);
			}
			catch (Exception ex)
			{
				_log.LogError(ex, "Error en ciclo de polling");
			}

			await Task.Delay(Intervalo, stoppingToken);
		}
	}

	/// <summary>Obtiene la lista de impresoras activas desde la app.</summary>
	private async Task RefrescarImpresorasAsync(CancellationToken ct)
	{
		var http = _factory.CreateClient("cloud");
		var lista = await http.GetFromJsonAsync<List<ImpresoraDto>>("/api/print/impresoras", ct);
		if (lista is not null && lista.Count > 0)
		{
			_impresoras = lista;
			_log.LogDebug("Impresoras cargadas desde app: {n}", lista.Count);
		}
	}

	private async Task ProcesarTrabajosPendientesAsync(CancellationToken ct)
	{
		var http = _factory.CreateClient("cloud");

		var trabajos = await http.GetFromJsonAsync<List<TrabajoPrintDto>>(
			"/api/print/pendientes", ct);

		if (trabajos is null || trabajos.Count == 0) return;

		foreach (var t in trabajos)
		{
			try
			{
				var bytes     = Convert.FromBase64String(t.ContenidoBase64);
				var impresora = ObtenerNombreImpresora(t.DestinoRol);
				Imprimir(bytes, impresora, t.Referencia);

				await http.PostAsync($"/api/print/{t.Id}/impreso", null, ct);
				_log.LogInformation("Trabajo {Id} ({Ref}) impreso en '{Imp}'", t.Id, t.Referencia, impresora);
			}
			catch (Exception ex)
			{
				_log.LogWarning(ex, "No se pudo imprimir trabajo {Id}", t.Id);
				await http.PostAsync($"/api/print/{t.Id}/error", null, ct);
			}
		}
	}

	/// <summary>
	/// Busca la impresora para el rol dado en la lista cargada desde la app.
	/// Primero busca por rol exacto; si no hay, usa la de rol Todas (2).
	/// Como fallback final usa "Printers:Todas" del appsettings.json local.
	/// </summary>
	private string ObtenerNombreImpresora(int rolDestino)
	{
		// Buscar en las impresoras cargadas desde la app
		var exacta = _impresoras.FirstOrDefault(i => i.Rol == rolDestino);
		if (exacta is not null) return exacta.Nombre;

		var todas = _impresoras.FirstOrDefault(i => i.Rol == 2);
		if (todas is not null) return todas.Nombre;

		// Fallback al appsettings local (compatibilidad)
		string seccion = rolDestino switch { 0 => "Barra", 1 => "Cocina", _ => "Todas" };
		return _cfg[$"Printers:{seccion}"]
			?? _cfg["Printers:Todas"]
			?? throw new InvalidOperationException(
				$"No hay impresora configurada para el rol '{seccion}'. " +
				"Añádela en la pantalla Impresoras de la app.");
	}

	private static void Imprimir(byte[] bytes, string nombreImpresora, string docNombre)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			RawPrinterHelper.EnviarBytesRaw(nombreImpresora, docNombre, bytes);
		else
			File.WriteAllBytes(nombreImpresora, bytes);
	}
}

/// <summary>DTO recibido desde GET /api/print/impresoras</summary>
public record ImpresoraDto(int Id, string Nombre, string Direccion, int Rol, bool ImprimeFacturas);
