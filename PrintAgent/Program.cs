using PrintAgent;

var builder = Host.CreateApplicationBuilder(args);

// Lee la URL del backend desde appsettings.json o variables de entorno
builder.Services.AddHttpClient("cloud", c =>
{
	c.BaseAddress = new Uri(builder.Configuration["CloudUrl"] ?? "https://localhost:5001");
});

builder.Services.AddHostedService<PrintWorker>();

await builder.Build().RunAsync();
