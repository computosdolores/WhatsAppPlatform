using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WhatsAppGateway.Configuration;
using WhatsAppGateway.Models;

namespace WhatsAppGateway.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppOptions _options;

    public WhatsAppService(HttpClient httpClient, IOptions<WhatsAppOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }
    public async Task<WhatsAppResponse> EnviarTextoAsync(EnviarTextoRequest request)
    {
        var url = $"{_options.BaseUrl}/{_options.ApiVersion}/{_options.PhoneNumberId}/messages";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

        var body = new
        {
            messaging_product = "whatsapp",
            to = request.Telefono,
            type = "text",
            text = new
            {
                body = request.Mensaje
            }
        };

        var json = JsonSerializer.Serialize(body);

        var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(url, content);

        var respuestaMeta = await response.Content.ReadAsStringAsync();

        return new WhatsAppResponse
        {
            Exito = response.IsSuccessStatusCode,
            Mensaje = response.IsSuccessStatusCode
                ? "Mensaje enviado correctamente."
                : "Error al enviar.",
            MetaResponse = respuestaMeta
        };
    }
    public async Task<WhatsAppResponse> EnviarImagenAsync(EnviarImagenRequest request)
    {
        var url = $"{_options.BaseUrl}/{_options.ApiVersion}/{_options.PhoneNumberId}/messages";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

        var body = new
        {


            messaging_product = "whatsapp",
            to = request.Telefono,
            type = "image",
            image = new
            {
                link = request.UrlImagen,
                caption = request.Caption
            }
        };

        var json = JsonSerializer.Serialize(body);

        var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(url, content);

        var respuestaMeta = await response.Content.ReadAsStringAsync();

        return new WhatsAppResponse
        {
            Exito = response.IsSuccessStatusCode,
            Mensaje = response.IsSuccessStatusCode
                ? "Imagen enviada correctamente."
                : "Error al enviar imagen.",
            MetaResponse = respuestaMeta
        };
    }
    public async Task<WhatsAppResponse> EnviarPlantillaAsync(EnviarPlantillaRequest request)
    {
        var url =
            $"{_options.BaseUrl}/{_options.ApiVersion}/{_options.PhoneNumberId}/messages";

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _options.AccessToken);

        var body = new
        {
            messaging_product = "whatsapp",

            to = request.Telefono,

            type = "template",

            template = new
            {
                name = "feliz_cumpleanos",

                language = new
                {
                    code = "es"
                },

                components = new object[]
                {
                // ==========================================
                // HEADER - IMAGEN
                // ==========================================

                new
                {
                    type = "header",

                    parameters = new object[]
                    {
                        new
                        {
                            type = "image",

                            image = new
                            {
                                link = request.UrlImagen
                            }
                        }
                    }
                },

                // ==========================================
                // BODY - NOMBRE
                // ==========================================

                new
                {
                    type = "body",

                    parameters = new object[]
                    {
                        new
                        {
                            type = "text",

                            text = request.Nombre
                        }
                    }
                }
                }
            }
        };

        var json = JsonSerializer.Serialize(body);

        var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            url,
            content);

        var respuestaMeta =
            await response.Content.ReadAsStringAsync();

        return new WhatsAppResponse
        {
            Exito = response.IsSuccessStatusCode,

            Mensaje = response.IsSuccessStatusCode
                ? "Plantilla enviada correctamente."
                : "Error al enviar plantilla.",

            MetaResponse = respuestaMeta
        };
    }
}