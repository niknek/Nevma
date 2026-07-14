using Nevma.Messaging.Api;
using Nevma.Messaging.Api.Endpoints;
using Nevma.Messaging.Api.Realtime;
using Nevma.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddNevmaServiceDefaults();
builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 32 * 1024);
builder.Services.AddMessagingService();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseNevmaServiceDefaults();
app.MapHealthChecks("/health");
app.MapHub<ChatHub>("/hubs/chat");
app.MapConversationEndpoints();

app.Run();
