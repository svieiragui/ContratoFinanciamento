# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/ContractsApi.Api/ContractsApi.Api.csproj", "src/ContractsApi.Api/"]
COPY ["src/ContractsApi.Application/ContractsApi.Application.csproj", "src/ContractsApi.Application/"]
COPY ["src/ContractsApi.Infrastructure/ContractsApi.Infrastructure.csproj", "src/ContractsApi.Infrastructure/"]
COPY ["src/ContractsApi.Domain/ContractsApi.Domain.csproj", "src/ContractsApi.Domain/"]

# Restore dependencies
RUN dotnet restore "src/ContractsApi.Api/ContractsApi.Api.csproj"

# Copy remaining source code
COPY . .

# Build application
WORKDIR "/src/src/ContractsApi.Api"
RUN dotnet build "ContractsApi.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "ContractsApi.Api.csproj" -c Release -o /app/publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p logs

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD dotnet tool install -g dotnet-trace 2>/dev/null; curl -f http://localhost:8080/health || exit 1

# Run application
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ContractsApi.Api.dll"]
