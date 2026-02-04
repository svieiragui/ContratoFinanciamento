using ContractsApi.Api.Configuration;
using ContractsApi.Api.Middlewares;
using ContractsApi.Application.Features.Clientes.GetResumo;
using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Application.Features.ContratosFinanciamento.Delete;
using ContractsApi.Application.Features.ContratosFinanciamento.GetAll;
using ContractsApi.Application.Features.ContratosFinanciamento.GetById;
using ContractsApi.Application.Features.Pagamentos.Create;
using ContractsApi.Application.Features.Pagamentos.GetByContrato;
using ContractsApi.Domain.Repositories;
using ContractsApi.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Configurações
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<FixedUserSettings>(builder.Configuration.GetSection("FixedUser"));

// Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Repositórios
builder.Services.AddScoped<IContratoFinanciamentoRepository>(sp => new ContratoFinanciamentoRepository(connectionString));
builder.Services.AddScoped<IPagamentoRepository>(sp => new PagamentoRepository(connectionString));

// Handlers - Contratos Financiamento
builder.Services.AddScoped<CreateContratoHandler>();
builder.Services.AddScoped<GetAllContratosHandler>();
builder.Services.AddScoped<GetContratoByIdHandler>();
builder.Services.AddScoped<DeleteContratoHandler>();

// Handlers - Pagamentos
builder.Services.AddScoped<CreatePagamentoHandler>();
builder.Services.AddScoped<GetPagamentosByContratoHandler>();

// Handlers - Clientes
builder.Services.AddScoped<GetResumoClienteHandler>();

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<CreateContratoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreatePagamentoValidator>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings not found.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
    };
});

builder.Services.AddAuthorization();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql");

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Contracts API - Sistema de Financiamento",
        Version = "v1",
        Description = "API para gerenciamento de contratos de financiamento, pagamentos e resumo de clientes"
    });

    // Incluir comentários XML
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Configuração JWT no Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Middleware de Exception Global
app.UseMiddleware<GlobalExceptionMiddleware>();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Contracts API v1");
        c.RoutePrefix = string.Empty; // Swagger na raiz
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health Check Endpoint
app.MapHealthChecks("/health");

try
{
    Log.Information("Starting Contracts API - Sistema de Financiamento");
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

// Necessário para testes de integração
public partial class Program { }