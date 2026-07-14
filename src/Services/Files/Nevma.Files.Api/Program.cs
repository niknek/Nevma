using Nevma.Files.Api;
using Nevma.Files.Api.Endpoints;
using Nevma.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddNevmaServiceDefaults(builder.Configuration);
builder.Services.AddFilesService(builder.Configuration, builder.Environment);

var app = builder.Build();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.UseNevmaServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapFileEndpoints();
app.Run();

public partial class Program;
