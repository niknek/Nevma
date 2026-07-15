using Nevma.Notifications.Api;
using Nevma.Notifications.Api.Endpoints;
using Nevma.Notifications.Api.Realtime;
using Nevma.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddNevmaServiceDefaults(builder.Configuration);
builder.Services.AddNotificationsService(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseNevmaServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapNotificationEndpoints();
app.MapPushDeviceEndpoints();
app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization();

app.Run();
