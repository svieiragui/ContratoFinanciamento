using ContractsApi.Domain.Repositories;
using ContractsApi.Infrastructure.Data;
using ContractsApi.Infrastructure.Repositories;

namespace ContractsApi.Api.Startup
{
    public static class RepositoriesConfiguration
    {
        public static void ConfigureRepositories(this WebApplicationBuilder builder)
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddScoped<IDbConnectionFactory>(sp => new DbConnectionFactory(connectionString));
            builder.Services.AddScoped<IContratoFinanciamentoRepository, ContratoFinanciamentoRepository>();
            builder.Services.AddScoped<IPagamentoRepository, PagamentoRepository>();
        }
    }
}
