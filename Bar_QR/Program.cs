using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Asegurar que las variables de entorno se cargan (Railway las inyecta como env vars del SO)
builder.Configuration.AddEnvironmentVariables();

// Configurar cadena SQLite y puerto desde entorno (Railway)
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portEnv) && int.TryParse(portEnv, out var p))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{p}");
}

var sqlitePath = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=/data/barqr.db";
// Si el directorio /data no existe (local dev), usar directorio actual
if (sqlitePath.Contains("/data/") && !Directory.Exists("/data"))
{
    sqlitePath = "Data Source=barqr.db";
}

builder.Services.AddSingleton(new Func<string>(() => sqlitePath));
// Registrar DbContext con SQLite
builder.Services.AddDbContext<Bar_QR.Data.AppDbContext>(options =>
{
    options.UseSqlite(sqlitePath);
});

var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=barqr.db";

// Add services to the container.
builder.Services.AddControllersWithViews();

// DataProtection base (necesario para cookies de sesión)
builder.Services.AddDataProtection()
	.SetApplicationName("Bar_QR");

// Configurar ForwardedHeaders para Railway (proxy SSL termination)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
	options.KnownNetworks.Clear();
	options.KnownProxies.Clear();
});

// --- PASO 1: CONFIGURACI
builder.Services.AddAuthentication("CookieAuth")
	.AddCookie("CookieAuth", config =>
	{
		config.Cookie.Name = "BarQR.Session";
		config.LoginPath = "/Login/Index";
		config.AccessDeniedPath = "/Login/Index";
	})
	.AddCookie("External", o =>
	{
		o.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
		o.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
	})
	.AddGoogle("Google", options =>
	{
		options.SignInScheme = "External";
		options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "no-configurado";
		options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "no-configurado";
		options.CallbackPath = "/Login/GoogleCallback";
		options.CorrelationCookie.Path = "/";
		options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
		options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
		options.CorrelationCookie.HttpOnly = true;
		options.CorrelationCookie.IsEssential = true;
		options.CorrelationCookie.Name = ".Correlation.Google";
		// Usar proveedor de DataProtection con clave fija para que la correlación
		// sobreviva reinicios de contenedor en Railway
		var secret = Environment.GetEnvironmentVariable("OAUTH_SECRET") ?? "fallback-dev-secret-no-usar-en-prod";
		options.DataProtectionProvider = new Bar_QR.Utils.EnvKeyDataProtectionProvider(secret);
		options.Events.OnRemoteFailure = ctx =>
		{
			var ex = ctx.Failure;
			Console.Error.WriteLine($"[OAuth] RemoteFailure: {ex}");
			Console.Error.WriteLine($"[OAuth] Stack: {ex?.StackTrace}");
			var inner = ex?.InnerException?.Message ?? "";
			var msg = ex?.Message ?? "error desconocido";
			var full = string.IsNullOrEmpty(inner) ? msg : $"{msg} | {inner}";
			ctx.Response.Redirect("/Login/Index?oauthError=" + Uri.EscapeDataString(full));
			ctx.HandleResponse();
			return Task.CompletedTask;
		};
	});
// ----------------------------------------------------

var app = builder.Build();

// Inicializar DB y seed
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
    Bar_QR.Data.DbInitializer.Initialize(db);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[Startup] Error al inicializar la base de datos: {ex.Message}");
}

// Railway termina SSL en el proxy ? ForwardedHeaders lee X-Forwarded-Proto del proxy.
app.UseForwardedHeaders();

// Fallback: forzar HTTPS si ForwardedHeaders no lo estableció (seguridad extra para Railway).
app.Use((context, next) =>
{
    context.Request.Scheme = "https";
    return next(context);
});

// LOG diagnóstico OAuth
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Login/GoogleCallback"))
    {
        var cookies = string.Join(", ", context.Request.Cookies.Keys);
        Console.Error.WriteLine($"[Callback] Scheme={context.Request.Scheme}");
        Console.Error.WriteLine($"[Callback] Cookies: {(string.IsNullOrEmpty(cookies) ? "NINGUNA" : cookies)}");
        Console.Error.WriteLine($"[Callback] Query: {context.Request.QueryString}");
    }
    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}
else
{
	app.UseHttpsRedirection();
}

app.MapStaticAssets();

// Middleware para detección automática de personal vía header (opcional)
// Si un proxy o la red local añade la cabecera X-Staff-Email, intentamos autenticar
app.Use(async (context, next) =>
{
    if (!context.User.Identity.IsAuthenticated)
    {
    if (context.Request.Headers.TryGetValue("X-Staff-Email", out var valores))
        {
            var email = valores.FirstOrDefault();
            if (!string.IsNullOrEmpty(email))
            {
                // Comprueba en la lista administrada por la base de datos
                using (var scope2 = context.RequestServices.CreateScope())
                {
                    var db2 = scope2.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
                    if (!db2.StaffEmails.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                    {
                        await next();
                        return;
                    }
                }
                {
                    // Verificar IP remota y si la petición viene desde un proxy confiable
                    string remoteIp = context.Connection.RemoteIpAddress?.ToString();
                    // Si hay X-Forwarded-For, solo lo respetamos si la conexión proviene de un proxy confiable
                    if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var xff))
                    {
                        var proxyIp = context.Connection.RemoteIpAddress?.ToString();
                        if (!string.IsNullOrEmpty(proxyIp) && Bar_QR.Controllers.AdminController.ListaProxies.Any(p => Bar_QR.Utils.IpRangeHelper.IsInRange(p, proxyIp)))
                        {
                            var first = xff.ToString().Split(',').FirstOrDefault()?.Trim();
                            if (!string.IsNullOrEmpty(first)) remoteIp = first;
                        }
                    }

                    if (!string.IsNullOrEmpty(remoteIp) && Bar_QR.Controllers.AdminController.ListaIPsStaff.Any(ip => Bar_QR.Utils.IpRangeHelper.IsInRange(ip, remoteIp)))
                    {
                        // Emite cookie de autenticación para camarero
                        var claims = new List<System.Security.Claims.Claim> {
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, email),
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Camarero")
                        };
                        var identity = new System.Security.Claims.ClaimsIdentity(claims, "CookieAuth");
                        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(365)
                        };
                        await context.SignInAsync("CookieAuth", principal, authProperties);
                    }
                    else
                    {
                        // IP no permitida: no autenticar
                    }
                }
            }
        }
    }
    await next();
});

app.UseRouting();

// --- PASO 2: ACTIVAR LA LLAVE Y EL CANDADO ---
// ¡El orden es sagrado! Primero Authentication, luego Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Comprobar si la base de datos tiene mesas y crear 10 por defecto si está vacía
try
{
    using var scope2 = app.Services.CreateScope();
    var appDbContext = scope2.ServiceProvider.GetRequiredService<Bar_QR.Data.AppDbContext>();
    if (!appDbContext.Mesas.Any())
    {
        for (int i = 1; i <= 10; i++)
        {
            appDbContext.Mesas.Add(new Bar_QR.Models.Mesa
            {
                NumeroMesa = i,
                Estado = Bar_QR.Models.EstadoMesa.Libre
            });
        }
        appDbContext.SaveChanges();
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[Startup] Error al crear mesas por defecto: {ex.Message}");
}

app.Run();