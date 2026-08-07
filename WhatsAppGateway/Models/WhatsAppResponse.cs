namespace WhatsAppGateway.Models;

public class WhatsAppResponse
{
    public bool Exito { get; set; }

    public string Mensaje { get; set; } = string.Empty;

    public string? MetaResponse { get; set; }
}