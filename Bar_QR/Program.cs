using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Asegurar que las variables de entorno se cargan (Railway las inyecta como env vars del SO)
builder.Configuration.AddEnvironmentVariables();

var sqlitePath = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=barqr.db";

builder.Services.AddSingleton(new Func<string>(() => sqlitePath));
// Registrar DbContext con SQLite
builder.Services.AddDbContext<Bar_QR.Data.AppDbContext>(options =>
{
    options.UseSqlite(sqlitePath);
});

var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=barqr.db";

// Add services to the container.
builder.Services.AddControllersWithViews();

// --- PASO 1: CONFIGURACIÓN DE SEGURIDAD (COOKIES) ---
builder.Services.AddAuthentication("CookieAuth")
	.AddCookie("CookieAuth", config =>
	{
		config.Cookie.Name = "BarQR.Session";
		config.LoginPath = "/Login/Index";
		config.AccessDeniedPath = "/Login/Index";
	})
	.AddGoogle("Google", options =>
	{
		options.SignInScheme = "CookieAuth";
		options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "no-configurado";
		options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "no-configurado";
		options.CallbackPath = "/Login/GoogleCallback";
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

// --- MAPEO DE ACTIVOS ESTÁTICOS (NUEVO EN .NET 9) ---
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