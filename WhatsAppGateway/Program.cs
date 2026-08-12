using Microsoft.AspNetCore.Mvc;
using WhatsAppGateway.Configuration;
using WhatsAppGateway.Endpoints;
using WhatsAppGateway.Services;
using WhatsAppGateway.Models;

Environment.SetEnvironmentVariable(
    "DOTNET_USE_POLLING_FILE_WATCHER",
    "1");

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

var token = builder.Configuration["WhatsApp:AccessToken"];

Console.WriteLine(
    $"TOKEN CARGADO: {(string.IsNullOrEmpty(token) ? "NO" : "SI")}"
);

builder.Services.Configure<WhatsAppOptions>(
    builder.Configuration.GetSection("WhatsApp"));


// ==========================================
// APLICACIÓN
// ==========================================

var app = builder.Build();


// ==========================================
// ARCHIVOS ESTÁTICOS
// ==========================================

app.UseStaticFiles();


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
        version = "2.0 WHATSAPP ENDPOINTS",
        fecha = DateTime.UtcNow
    };
});


// ==========================================
// ENDPOINTS
// ==========================================

app.MapTestEndpoint();

app.MapWhatsAppEndpoint();


// ==========================================
// SUBIR IMAGEN
// ==========================================

app.MapPost("/api/whatsapp/subir-imagen",
    async (
        [FromForm] SubirImagenRequest request,
        IWebHostEnvironment environment,
        HttpContext httpContext) =>
    {
        var archivo = request.Archivo;

        Console.WriteLine("====================================");
        Console.WriteLine("SUBIR IMAGEN");
        Console.WriteLine($"Archivo: {archivo?.FileName}");
        Console.WriteLine($"Tamaño: {archivo?.Length}");
        Console.WriteLine($"ContentType: {archivo?.ContentType}");
        Console.WriteLine("====================================");

        if (archivo == null || archivo.Length == 0)
        {
            return Results.BadRequest(new
            {
                exito = false,
                mensaje = "No se recibió ninguna imagen."
            });
        }

        if (string.IsNullOrWhiteSpace(archivo.ContentType) ||
            !archivo.ContentType.StartsWith(
                "image/",
                StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new
            {
                exito = false,
                mensaje = $"El archivo recibido no es una imagen. ContentType: {archivo.ContentType}"
            });
        }

        string carpetaImagenes = Path.Combine(
            environment.WebRootPath
                ?? Path.Combine(
                    environment.ContentRootPath,
                    "wwwroot"),
            "imagenes");

        Directory.CreateDirectory(carpetaImagenes);

        string extension = Path.GetExtension(archivo.FileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        string nombreArchivo =
            $"{Guid.NewGuid():N}{extension}";

        string rutaArchivo =
            Path.Combine(
                carpetaImagenes,
                nombreArchivo);

        await using (var stream = new FileStream(
            rutaArchivo,
            FileMode.Create))
        {
            await archivo.CopyToAsync(stream);
        }

        string url =
            $"{httpContext.Request.Scheme}://" +
            $"{httpContext.Request.Host}" +
            $"/imagenes/{nombreArchivo}";

        return Results.Ok(new
        {
            exito = true,
            url
        });
    })
    .Accepts<SubirImagenRequest>("multipart/form-data")
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("SubirImagen")
    .WithOpenApi()
    .DisableAntiforgery();


// ==========================================
// EJECUTAR
// ==========================================

app.Run();