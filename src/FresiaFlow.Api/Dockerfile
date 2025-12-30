# Dockerfile para FresiaFlow.Api (.NET 8.0)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivo de solución
COPY ["FresiaFlow.sln", "./"]

# Copiar archivos de proyectos para restaurar dependencias
COPY ["src/FresiaFlow.Api/FresiaFlow.Api.csproj", "src/FresiaFlow.Api/"]
COPY ["src/FresiaFlow.Adapters/FresiaFlow.Adapters.csproj", "src/FresiaFlow.Adapters/"]
COPY ["src/FresiaFlow.Application/FresiaFlow.Application.csproj", "src/FresiaFlow.Application/"]
COPY ["src/FresiaFlow.Domain/FresiaFlow.Domain.csproj", "src/FresiaFlow.Domain/"]
COPY ["src/FresiaFlow.Infrastructure/FresiaFlow.Infrastructure.csproj", "src/FresiaFlow.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "FresiaFlow.sln"

# Copiar todo el código fuente
COPY . .

# Build de la aplicación
WORKDIR "/src/src/FresiaFlow.Api"
RUN dotnet build "FresiaFlow.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FresiaFlow.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Crear directorios necesarios
RUN mkdir -p /app/uploads /app/incoming-invoices /app/processed/success /app/processed/error

ENTRYPOINT ["dotnet", "FresiaFlow.Api.dll"]

