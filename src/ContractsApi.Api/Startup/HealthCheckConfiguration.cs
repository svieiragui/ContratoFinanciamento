namespace ContractsApi.Api.Startup
{
    public static class HealthCheckConfiguration
    {
        public static void ConfigureHealthChecks(this WebApplicationBuilder builder)
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddHealthChecks()
                .AddNpgSql(connectionString, name: "postgresql");
        }
    }
}
