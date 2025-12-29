# Configuración de Base de Datos — FresiaFlow

## Requisitos

- PostgreSQL 14+ corriendo en `localhost:5432`
- Base de datos: `fresiaflow` (se crea automáticamente)
- Usuario: `postgres` / Password: `Fresia_31`

## Migraciones

### Primera Vez

```bash
# 1. Crear migración inicial
cd src/FresiaFlow.Api
dotnet ef migrations add InitialCreate --project ../FresiaFlow.Adapters --context FresiaFlowDbContext --output-dir Outbound/Persistence/Migrations

# 2. Aplicar migración (OPCIONAL - se aplica automáticamente en desarrollo)
dotnet ef database update --project ../FresiaFlow.Adapters
```

### Agregar Nueva Migración

Cuando cambies entidades en `Domain`:

```bash
cd src/FresiaFlow.Api
dotnet ef migrations add <NombreMigración> --project ../FresiaFlow.Adapters --context FresiaFlowDbContext --output-dir Outbound/Persistence/Migrations
```

Ejemplo:
```bash
dotnet ef migrations add AddInvoiceReceivedStatus --project ../FresiaFlow.Adapters --context FresiaFlowDbContext --output-dir Outbound/Persistence/Migrations
```

### Ver Migraciones Pendientes

```bash
cd src/FresiaFlow.Api
dotnet ef migrations list --project ../FresiaFlow.Adapters
```

### Rollback (Deshacer Migración)

```bash
cd src/FresiaFlow.Api
dotnet ef database update <MigraciónAnterior> --project ../FresiaFlow.Adapters
```

### Eliminar Última Migración (NO aplicada aún)

```bash
cd src/FresiaFlow.Api
dotnet ef migrations remove --project ../FresiaFlow.Adapters
```

## Modo Desarrollo

**Automático:** Al iniciar la API, se aplican migraciones pendientes automáticamente.

Ver en `Program.cs`:
```csharp
if (app.Environment.IsDevelopment())
{
    context.Database.Migrate(); // Auto-migración
}
```

## Modo Producción

**Manual:** Ejecutar migración antes del deploy:

```bash
dotnet ef database update --connection "Host=prod-server;Database=fresiaflow;..." --project src/FresiaFlow.Adapters
```

## Comandos Útiles

### Ver SQL de Migración

```bash
dotnet ef migrations script --project src/FresiaFlow.Adapters
```

### Ver SQL de Migración Específica

```bash
dotnet ef migrations script <MigraciónAnterior> <MigraciónNueva> --project src/FresiaFlow.Adapters
```

### Generar Script para Producción

```bash
dotnet ef migrations script --idempotent --output migration.sql --project src/FresiaFlow.Adapters
```

(Genera SQL con `IF NOT EXISTS` para ser idempotente)

## Troubleshooting

### Error: "No migrations found"

```bash
# Asegúrate de estar en el directorio correcto
cd src/FresiaFlow.Api
dotnet ef migrations add InitialCreate --project ../FresiaFlow.Adapters --context FresiaFlowDbContext --output-dir Outbound/Persistence/Migrations
```

### Error: "The ConnectionString property has not been initialized"

Verificar `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=fresiaflow;Username=postgres;Password=Fresia_31"
  }
}
```

### Error: "Database does not exist"

PostgreSQL debe estar corriendo. Verificar:

```bash
# Windows
docker ps  # Si usas Docker
# O verifica servicios en Windows

# Crear BD manualmente si es necesario
psql -U postgres -c "CREATE DATABASE fresiaflow;"
```

### Error: "Already applied migrations"

Limpia y reaplica:

```bash
# SOLO EN DESARROLLO (borra datos)
dotnet ef database drop --project src/FresiaFlow.Adapters --force
dotnet ef database update --project src/FresiaFlow.Adapters
```

## Estructura de Migraciones

```
FresiaFlow.Adapters/
└── Outbound/
    └── Persistence/
        ├── Migrations/
        │   ├── 20250101120000_InitialCreate.cs           # Migración
        │   ├── 20250102080000_AddInvoiceStatus.cs        # Siguiente migración
        │   └── FresiaFlowDbContextModelSnapshot.cs       # Snapshot actual
        ├── Configurations/
        └── FresiaFlowDbContext.cs
```

## CI/CD (Futuro)

Para pipelines de deploy:

```yaml
# Ejemplo GitHub Actions
- name: Apply migrations
  run: |
    dotnet ef database update --project src/FresiaFlow.Adapters --connection "${{ secrets.DB_CONNECTION }}"
```

## Referencias

- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostgreSQL + EF Core](https://www.npgsql.org/efcore/)

