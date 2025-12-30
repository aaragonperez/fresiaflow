using FresiaFlow.Infrastructure;
using FresiaFlow.Adapters.Outbound.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors();

// Configurar FresiaFlow
builder.Services.AddFresiaFlowInfrastructure(builder.Configuration);

var app = builder.Build();

// Aplicar migraciones automáticamente (desarrollo y Docker)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FresiaFlowDbContext>();
    
    try
    {
        context.Database.Migrate();
        app.Logger.LogInformation("✅ Migraciones aplicadas correctamente");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "❌ Error aplicando migraciones");
        throw;
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Habilitar Swagger también en producción para desarrollo local
if (!app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Comentar redirect HTTPS para desarrollo local
// app.UseHttpsRedirection();

// CORS DEBE estar ANTES de UseAuthorization y MapHub para SignalR
// Permitir tanto localhost (desarrollo) como el contenedor web (Docker)
app.UseCors(policy => policy
    .WithOrigins("http://localhost:4200", "http://web:80", "http://localhost")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

app.UseRouting(); // Necesario para SignalR

app.UseAuthorization();

// MapHub debe estar dentro de UseEndpoints o después de UseRouting
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<FresiaFlow.Adapters.Inbound.Api.Hubs.SyncProgressHub>("/hubs/sync-progress");
});

// Configurar puerto 5000 (0.0.0.0 para Docker, localhost para desarrollo local)
var port = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?.Contains("+") == true 
    ? "http://+:5000" 
    : "http://0.0.0.0:5000";
app.Urls.Add(port);

app.Run();

