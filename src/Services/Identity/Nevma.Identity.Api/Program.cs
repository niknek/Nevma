using Nevma.Identity.Api;
using Nevma.Identity.Api.Endpoints;
using Nevma.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddNevmaServiceDefaults();
builder.Services.AddIdentityService();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseNevmaServiceDefaults();
app.MapHealthChecks("/health");
app.MapUserEndpoints();

app.Run();
