using WhatsAppGateway.Models;
using WhatsAppGateway.Services;

namespace WhatsAppGateway.Endpoints;

public static class WhatsAppEndpoint
{
    public static void MapWhatsAppEndpoint(this WebApplication app)
    {
        app.MapPost("/api/whatsapp/enviar-texto",
            async (EnviarTextoRequest request, IWhatsAppService service) =>
            {
                var resultado = await service.EnviarTextoAsync(request);

                if (resultado.Exito)
                    return Results.Ok(resultado);

                return Results.BadRequest(resultado);
            })
            .WithName("EnviarTexto")
            .WithOpenApi();


        app.MapPost("/api/whatsapp/enviar-imagen",
            async (EnviarImagenRequest request, IWhatsAppService service) =>
            {
                var resultado = await service.EnviarImagenAsync(request);

                if (resultado.Exito)
                    return Results.Ok(resultado);

                return Results.BadRequest(resultado);
            })
            .WithName("EnviarImagen")
            .WithOpenApi();
    }
}