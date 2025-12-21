# FresiaFlow

Secretaria administrativa virtual para micro-pymes.

## Arquitectura

El sistema sigue **Arquitectura Hexagonal (Ports & Adapters)** estricta:

- **Domain**: Entidades, value objects y reglas de negocio (sin dependencias externas)
- **Application**: Casos de uso, puertos y orquestación de IA
- **Adapters**: Implementaciones de puertos (API, Persistence, OpenAI, Banking, etc.)
- **Infrastructure**: Configuración e inyección de dependencias

## Estructura del Proyecto

```
FresiaFlow/
├── src/                          # Backend (C# / ASP.NET Core)
│   ├── FresiaFlow.Domain/        # Dominio puro
│   ├── FresiaFlow.Application/    # Casos de uso y puertos
│   ├── FresiaFlow.Adapters/       # Adaptadores (API, Persistence, etc.)
│   ├── FresiaFlow.Infrastructure/ # DI y configuración
│   └── FresiaFlow.Api/            # API Web
├── apps/
│   ├── fresiaflow-web/            # Frontend Angular (hexagonal adaptada)
│   └── fresiaflow-mobile/         # Mobile Ionic + Angular (estructura)
└── FresiaFlow.sln                 # Solution file
```

## Stack Tecnológico

- **Backend**: C# / ASP.NET Core 8.0
- **Frontend Web**: Angular 17
- **Mobile**: Ionic + Angular
- **Base de datos**: PostgreSQL
- **ORM**: Entity Framework Core
- **IA**: OpenAI API
- **Bancos**: Open Banking AIS (TrueLayer, Plaid, etc.)

## Configuración

### Backend

1. Configurar `appsettings.json` con:
   - Connection string de PostgreSQL
   - OpenAI API Key
   - Banking API credentials

2. Ejecutar migraciones:
```bash
dotnet ef migrations add InitialCreate --project src/FresiaFlow.Adapters --startup-project src/FresiaFlow.Api
dotnet ef database update --project src/FresiaFlow.Adapters --startup-project src/FresiaFlow.Api
```

### Frontend

```bash
cd apps/fresiaflow-web
npm install
ng serve
```

## Funcionalidades Principales

- ✅ Gestión de facturas (PDF)
- ✅ Lectura de movimientos bancarios (Open Banking AIS)
- ✅ Conciliación bancaria
- ✅ Gestión de tareas (to-do diario)
- ✅ Orquestación con OpenAI API (Responses + tool calling)
- ✅ RAG con procedimientos internos

## Principios de Diseño

1. **El DOMINIO no depende de nada externo** (ni EF, ni HTTP, ni OpenAI)
2. **La IA NO vive en Controllers**; vive en Application como orquestador
3. **Todo acceso externo se hace vía PORTS + ADAPTERS**
4. **El frontend sigue hexagonal adaptada**: domain / application / ports / infrastructure / ui
5. **Nada de lógica de negocio en Controllers ni en componentes UI**

## Namespace Raíz

Todos los proyectos usan el namespace raíz: **FresiaFlow**

