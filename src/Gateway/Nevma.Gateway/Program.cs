using Nevma.Gateway;
using Nevma.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddNevmaServiceDefaults(builder.Configuration);
builder.Services.AddGateway(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseNevmaServiceDefaults();
app.UseCors("Gateway");
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    const long standardLimit = 1024 * 1024;
    const long uploadLimit = 26L * 1024 * 1024;
    if (context.Request.ContentLength >
        (context.Request.Path.StartsWithSegments("/files") ? uploadLimit : standardLimit))
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        return;
    }
    await next(context);
});
app.MapHealthChecks("/health");
app.MapReverseProxy();

app.Run();
