using Nevma.Planning.Api;
using Nevma.Planning.Api.Endpoints;
using Nevma.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddNevmaServiceDefaults();
builder.Services.AddPlanningService();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseNevmaServiceDefaults();
app.MapHealthChecks("/health");
app.MapMeetingInvitationEndpoints();
app.MapCalendarEndpoints();

app.Run();
