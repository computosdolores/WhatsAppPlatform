using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using WhatsAppGateway.Configuration;
using WhatsAppGateway.Endpoints;
using WhatsAppGateway.Models;
using WhatsAppGateway.Services;

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

// ==========================================
// DIAGNÓSTICO META - WABA Y PLANTILLAS
// ==========================================

app.MapGet("/api/whatsapp/diagnostico-plantillas",
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
                "AccessToken no configurado.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumberId))
        {
            return Results.Problem(
                "PhoneNumberId no configurado.");
        }

        try
        {
            var client =
                httpClientFactory.CreateClient();

            // ------------------------------------------
            // 1. OBTENER INFORMACIÓN DEL NÚMERO
            // ------------------------------------------

            string urlNumero =
                $"https://graph.facebook.com/{apiVersion}/{phoneNumberId}" +
                "?fields=id,display_phone_number,verified_name,waba_id";

            using var requestNumero =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    urlNumero);

            requestNumero.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            using var responseNumero =
                await client.SendAsync(requestNumero);

            string numeroJson =
                await responseNumero.Content.ReadAsStringAsync();

            if (!responseNumero.IsSuccessStatusCode)
            {
                return Results.Content(
                    numeroJson,
                    "application/json",
                    statusCode: (int)responseNumero.StatusCode);
            }

            using var numeroDoc =
                JsonDocument.Parse(numeroJson);

            if (!numeroDoc.RootElement.TryGetProperty(
                    "waba_id",
                    out var wabaElement))
            {
                return Results.Ok(new
                {
                    numero = numeroJson,
                    mensaje =
                        "Meta no devolvió waba_id en esta consulta."
                });
            }

            string wabaId =
                wabaElement.GetString() ?? "";

            // ------------------------------------------
            // 2. OBTENER PLANTILLAS
            // ------------------------------------------

            string urlPlantillas =
                $"https://graph.facebook.com/{apiVersion}/{wabaId}/message_templates";

            using var requestPlantillas =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    urlPlantillas);

            requestPlantillas.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            using var responsePlantillas =
                await client.SendAsync(requestPlantillas);

            string plantillasJson =
                await responsePlantillas.Content.ReadAsStringAsync();

            return Results.Ok(new
            {
                phone_number_id = phoneNumberId,

                waba_id = wabaId,

                numero = JsonDocument.Parse(
                    numeroJson).RootElement,

                plantillas = JsonDocument.Parse(
                    plantillasJson).RootElement
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERROR DIAGNOSTICO PLANTILLAS: {ex}");

            return Results.Problem(
                "Error consultando Meta.");
        }
    });

// ==========================================
// EJECUTAR
// ==========================================

app.Run();