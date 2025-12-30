# Docker Setup para FresiaFlow

Este documento explica cómo ejecutar FresiaFlow usando Docker y Docker Compose.

## Requisitos Previos

- Docker Desktop (o Docker Engine + Docker Compose)
- Al menos 4GB de RAM disponibles
- Puertos libres: 4200, 5000, 5432

## Estructura de Servicios

El `docker-compose.yml` incluye los siguientes servicios:

1. **postgres**: Base de datos PostgreSQL 16
2. **api**: Backend .NET 8.0 API
3. **web**: Frontend Angular servido con nginx

## Comandos Principales

### Iniciar todos los servicios

```bash
docker-compose up -d
```

### Ver logs de todos los servicios

```bash
docker-compose logs -f
```

### Ver logs de un servicio específico

```bash
docker-compose logs -f api
docker-compose logs -f web
docker-compose logs -f postgres
```

### Detener todos los servicios

```bash
docker-compose down
```

### Detener y eliminar volúmenes (incluyendo base de datos)

```bash
docker-compose down -v
```

### Reconstruir imágenes

```bash
docker-compose build --no-cache
```

### Reconstruir e iniciar

```bash
docker-compose up -d --build
```

## Acceso a los Servicios

Una vez iniciados los servicios:

- **Frontend Web**: http://localhost:4200
- **API Backend**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **PostgreSQL**: localhost:5432
  - Usuario: `postgres`
  - Contraseña: `Fresia_31`
  - Base de datos: `fresiaflow`

## Configuración

### Variables de Entorno

Las variables de entorno se pueden configurar en el archivo `docker-compose.yml` o mediante un archivo `.env`.

### Volúmenes

Los siguientes directorios están montados como volúmenes para persistencia:

- `./src/FresiaFlow.Api/uploads` → `/app/uploads` (archivos subidos)
- `./src/FresiaFlow.Api/incoming-invoices` → `/app/incoming-invoices` (facturas entrantes)
- `./src/FresiaFlow.Api/processed` → `/app/processed` (facturas procesadas)
- `postgres_data` → `/var/lib/postgresql/data` (datos de PostgreSQL)

### Migraciones de Base de Datos

Las migraciones de Entity Framework se aplican automáticamente al iniciar el servicio `api`.

## Desarrollo

### Modificar código y ver cambios

Para ver cambios en el código sin reconstruir:

1. **Backend**: Necesitas reconstruir la imagen:
   ```bash
   docker-compose up -d --build api
   ```

2. **Frontend**: Necesitas reconstruir la imagen:
   ```bash
   docker-compose up -d --build web
   ```

### Ejecutar comandos dentro de los contenedores

```bash
# Acceder al contenedor de la API
docker-compose exec api sh

# Acceder al contenedor de PostgreSQL
docker-compose exec postgres psql -U postgres -d fresiaflow

# Ejecutar migraciones manualmente (si es necesario)
docker-compose exec api dotnet ef database update
```

## Solución de Problemas

### El servicio no inicia

1. Verifica los logs: `docker-compose logs [servicio]`
2. Verifica que los puertos no estén en uso
3. Verifica que Docker tenga suficientes recursos asignados

### Error de conexión a la base de datos

1. Verifica que el servicio `postgres` esté saludable: `docker-compose ps`
2. Espera unos segundos después de iniciar para que PostgreSQL esté listo
3. Verifica la cadena de conexión en `appsettings.json`

### CORS errors en el frontend

1. Verifica que el servicio `api` esté corriendo
2. Verifica la configuración de CORS en `Program.cs`
3. Verifica que nginx esté configurado correctamente para el proxy

### Limpiar todo y empezar de nuevo

```bash
# Detener y eliminar contenedores, redes y volúmenes
docker-compose down -v

# Eliminar imágenes
docker rmi fresiaflow-api fresiaflow-web

# Reconstruir todo
docker-compose up -d --build
```

## Producción

Para producción, considera:

1. Usar variables de entorno para secretos (no hardcodear contraseñas)
2. Configurar HTTPS/TLS
3. Usar un reverse proxy (nginx/traefik) delante de los servicios
4. Configurar backups de la base de datos
5. Usar secrets de Docker para información sensible
6. Configurar healthchecks apropiados
7. Configurar límites de recursos (CPU, memoria)

