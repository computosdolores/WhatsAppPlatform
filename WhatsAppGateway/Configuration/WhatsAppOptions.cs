namespace WhatsAppGateway.Configuration;

public class WhatsAppOptions
{
    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v24.0";
    public string BaseUrl { get; set; } = "https://graph.facebook.com";
    public string UrlImagenCumple { get; set; } = string.Empty;
}