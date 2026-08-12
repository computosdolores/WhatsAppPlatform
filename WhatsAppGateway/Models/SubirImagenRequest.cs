using Microsoft.AspNetCore.Http;

namespace WhatsAppGateway.Models;

public class SubirImagenRequest
{
    public IFormFile Archivo { get; set; } = default!;
}