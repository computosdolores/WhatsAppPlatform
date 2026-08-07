namespace WhatsAppGateway.Endpoints
{
    public static class TestEndpoint
    {
        public static void MapTestEndpoint(this WebApplication app)
        {
            app.MapGet("/api/test", () =>
            {
                return Results.Ok(new
                {
                    ok = true,
                    mensaje = "WhatsApp Gateway funcionando correctamente",
                    fecha = DateTime.Now
                });
            })
            .WithName("Test")
            .WithOpenApi();
        }
    }
}
