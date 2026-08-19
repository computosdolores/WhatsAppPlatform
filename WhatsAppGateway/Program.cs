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

        string url = $"https://{httpContext.Request.Host}" + $"/imagenes/{nombreArchivo}";

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
// WEBHOOK WHATSAPP - VERIFICACIÓN META
// ==========================================

app.MapGet("/api/whatsapp/webhook",
    (
        HttpContext context,
        IConfiguration configuration) =>
    {
        string mode =
            context.Request.Query["hub.mode"].ToString();

        string token =
            context.Request.Query["hub.verify_token"].ToString();

        string challenge =
            context.Request.Query["hub.challenge"].ToString();

        string tokenConfigurado =
            configuration["WhatsApp:WebhookVerifyToken"] ?? "";

        Console.WriteLine("====================================");
        Console.WriteLine("VERIFICACIÓN WEBHOOK WHATSAPP");
        Console.WriteLine($"Mode: {mode}");
        Console.WriteLine($"Token recibido: {!string.IsNullOrEmpty(token)}");
        Console.WriteLine($"Token configurado: {!string.IsNullOrEmpty(tokenConfigurado)}");
        Console.WriteLine("====================================");

        if (mode == "subscribe" &&
            token == tokenConfigurado)
        {
            Console.WriteLine("✔ WEBHOOK VERIFICADO");

            return Results.Text(
                challenge,
                "text/plain");
        }

        Console.WriteLine("❌ ERROR DE VERIFICACIÓN");

        return Results.Unauthorized();
    });


// ==========================================
// WEBHOOK WHATSAPP - NOTIFICACIONES
// ==========================================

app.MapPost("/api/whatsapp/webhook",
    async (HttpRequest request) =>
    {
        using var reader =
            new StreamReader(request.Body);

        string body =
            await reader.ReadToEndAsync();

        Console.WriteLine("====================================");
        Console.WriteLine("WEBHOOK WHATSAPP RECIBIDO");
        Console.WriteLine(body);
        Console.WriteLine("====================================");

        return Results.Ok();
    });

// ==========================================
// DIAGNÓSTICO META WHATSAPP
// ==========================================

app.MapGet("/api/whatsapp/diagnostico",
    async (
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory) =>
    {
        string? accessToken =
            configuration["WhatsApp:AccessToken"];

        string? phoneNumberId =
            configuration["WhatsApp:PhoneNumberId"];

        string apiVersion =
            configuration["WhatsApp:ApiVersion"] ?? "v25.0";

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Results.Problem(
                "El AccessToken no está configurado.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumberId))
        {
            return Results.Problem(
                "El PhoneNumberId no está configurado.");
        }

        try
        {
            var client =
                httpClientFactory.CreateClient();

            string url =
                $"https://graph.facebook.com/{apiVersion}/{phoneNumberId}" +
                "?fields=id,display_phone_number,verified_name";

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    url);

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            using var response =
                await client.SendAsync(request);

            string contenido =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine("====================================");
            Console.WriteLine("DIAGNÓSTICO META WHATSAPP");
            Console.WriteLine($"PhoneNumberId: {phoneNumberId}");
            Console.WriteLine($"HTTP: {(int)response.StatusCode}");
            Console.WriteLine($"Respuesta: {contenido}");
            Console.WriteLine("====================================");

            return Results.Content(
                contenido,
                "application/json",
                statusCode: (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERROR DIAGNÓSTICO META: {ex.Message}");

            return Results.Problem(
                "Error al comunicarse con Meta.");
        }
    });

// ==========================================
// EJECUTAR
// ==========================================

app.Run();