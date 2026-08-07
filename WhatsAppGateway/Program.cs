using WhatsAppGateway.Services;
using WhatsAppGateway.Configuration;

Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    EnvironmentName = Environments.Production
});


// ==========================================
// PUERTO RENDER
// ==========================================

var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");


// ==========================================
// SERVICIOS
// ==========================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();

builder.Services.Configure<WhatsAppOptions>(
    builder.Configuration.GetSection("WhatsApp"));


// ==========================================
// APLICACIÓN
// ==========================================

var app = builder.Build();


// ==========================================
// SWAGGER
// ==========================================

app.UseSwagger();
app.UseSwaggerUI();


// ==========================================
// ENDPOINT PRUEBA
// ==========================================

app.MapGet("/", () =>
{
    return new
    {
        estado = "OK",
        servicio = "WhatsAppGateway",
        fecha = DateTime.UtcNow
    };
});


// ==========================================
// EJECUTAR
// ==========================================

app.Run();