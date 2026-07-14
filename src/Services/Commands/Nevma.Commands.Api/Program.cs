using Nevma.Commands.Api;
using Nevma.Commands.Api.Endpoints;
using Nevma.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi(); builder.Services.AddNevmaServiceDefaults(builder.Configuration); builder.Services.AddCommandsService(builder.Configuration);
var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseNevmaServiceDefaults(); app.UseAuthentication(); app.UseAuthorization();
app.MapHealthChecks("/health"); app.MapCommandEndpoints(); app.Run();
public partial class Program;
