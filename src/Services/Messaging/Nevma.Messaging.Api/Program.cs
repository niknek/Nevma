using Nevma.Messaging.Api;
using Nevma.Messaging.Api.Endpoints;
using Nevma.Messaging.Api.Realtime;
using Nevma.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddNevmaServiceDefaults(builder.Configuration);
builder.Services.AddMessagingService(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseNevmaServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();
app.MapConversationEndpoints();

app.Run();
