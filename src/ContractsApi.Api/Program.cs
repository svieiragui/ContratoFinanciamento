using ContractsApi.Api.Middlewares;
using ContractsApi.Api.Startup;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging Configuration
builder.ConfigureLogging();

// Application Services Configuration
builder.ConfigureApplicationServices();

// Repositories Configuration
builder.ConfigureRepositories();

// Authentication Configuration
builder.ConfigureAuthentication();

// Health Checks Configuration
builder.ConfigureHealthChecks();

// Swagger Configuration
builder.ConfigureSwagger();

// Controllers
builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

try
{
    Log.Information("Starting Contracts API with Fire-and-Forget");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }