namespace WhatsAppGateway.Models;

public class EnviarTextoRequest
{
    public string Telefono { get; set; } = string.Empty;

    public string Mensaje { get; set; } = string.Empty;
}