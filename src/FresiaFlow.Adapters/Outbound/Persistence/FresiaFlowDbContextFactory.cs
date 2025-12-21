using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FresiaFlow.Adapters.Outbound.Persistence;

/// <summary>
/// Factory para crear el DbContext en tiempo de diseño (migraciones).
/// </summary>
public class FresiaFlowDbContextFactory : IDesignTimeDbContextFactory<FresiaFlowDbContext>
{
    public FresiaFlowDbContext CreateDbContext(string[] args)
    {
        // Construir configuración desde appsettings.json del proyecto Api
        // El directorio actual será el del proyecto Adapters, necesitamos ir al Api
        var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "FresiaFlow.Api"));
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "No se encontró la connection string 'DefaultConnection' en appsettings.json. " +
                "Asegúrate de que el archivo appsettings.json existe en el proyecto FresiaFlow.Api.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<FresiaFlowDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new FresiaFlowDbContext(optionsBuilder.Options);
    }
}

