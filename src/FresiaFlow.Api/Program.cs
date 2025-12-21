using FresiaFlow.Infrastructure;
using FresiaFlow.Adapters.Outbound.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors();

// Configurar FresiaFlow
builder.Services.AddFresiaFlowInfrastructure(builder.Configuration);

var app = builder.Build();

// Aplicar migraciones automáticamente en desarrollo
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
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

app.UseHttpsRedirection();

// CORS para permitir peticiones desde el frontend
app.UseCors(policy => policy
    .WithOrigins("http://localhost:4200")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

app.UseAuthorization();
app.MapControllers();

// Configurar puerto 5000
app.Urls.Add("http://localhost:5000");

app.Run();

