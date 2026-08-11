using WhatsAppGateway.Models;

namespace WhatsAppGateway.Services;

public interface IWhatsAppService
{
    Task<WhatsAppResponse> EnviarTextoAsync(EnviarTextoRequest request);
    Task<WhatsAppResponse> EnviarImagenAsync(EnviarImagenRequest request);
    Task<WhatsAppResponse> EnviarPlantillaAsync(EnviarPlantillaRequest request);
}