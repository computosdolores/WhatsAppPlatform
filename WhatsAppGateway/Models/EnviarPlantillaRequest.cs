namespace WhatsAppGateway.Models;

public class EnviarPlantillaRequest
{
    public string Telefono { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string UrlImagen { get; set; } = string.Empty;
}