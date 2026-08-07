using WhatsAppGateway.Endpoints;
using WhatsAppGateway.Services;
using WhatsAppGateway.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// PUERTO
// ==========================================

var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");


// ==========================================
// SERVICIOS
// ==========================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();

builder.Services.AddHttpClient();

builder.Services.Configure<WhatsAppOptions>(
    builder.Configuration.GetSection("WhatsApp"));


// ==========================================
// APLICACIÓN
// ==========================================

var app = builder.Build();


// ==========================================
// SWAGGER
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// ==========================================
// ARCHIVOS ESTÁTICOS
// ==========================================

app.UseStaticFiles();


// ==========================================
// ENDPOINTS
// ==========================================

app.MapTestEndpoint();
app.MapWhatsAppEndpoint();


// ==========================================
// EJECUTAR
// ==========================================

app.Run();