using MediatR;
using ContractsApi.Application.Features.Clientes.GetResumo;
using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Application.Features.ContratosFinanciamento.Delete;
using ContractsApi.Application.Features.ContratosFinanciamento.GetAll;
using ContractsApi.Application.Features.ContratosFinanciamento.GetById;
using ContractsApi.Application.Features.Pagamentos.Create;
using ContractsApi.Application.Features.Pagamentos.GetByContrato;
using FluentValidation;

namespace ContractsApi.Api.Startup
{
    public static class ApplicationServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddMediatR(typeof(CreatePagamentoHandler));

            ConfigureHandlers(builder);
            ConfigureValidators(builder);
        }

        private static void ConfigureHandlers(WebApplicationBuilder builder)
        {
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
        }

        private static void ConfigureValidators(WebApplicationBuilder builder)
        {
            builder.Services.AddValidatorsFromAssemblyContaining<CreateContratoValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreatePagamentoValidator>();
        }
    }
}
