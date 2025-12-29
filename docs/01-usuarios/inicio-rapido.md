# Inicio de FresiaFlow Web

## ✅ Configuración Completada

La aplicación Angular está configurada y lista para ejecutarse.

## 🚀 Comandos Disponibles

### Iniciar servidor de desarrollo
```bash
cd apps/fresiaflow-web
npm start
```

O desde la raíz del proyecto:
```bash
cd apps/fresiaflow-web && npm start
```

La aplicación estará disponible en: **http://localhost:4200**

### Build de producción
```bash
npm run build
```

## 📁 Estructura de Archivos

```
apps/fresiaflow-web/
├── src/                    # Código fuente principal
│   ├── main.ts            # Bootstrap de la aplicación
│   ├── app.component.ts   # Componente raíz
│   ├── app.routes.ts      # Configuración de rutas
│   └── styles.css         # Estilos globales
├── domain/                 # Modelos de dominio
├── application/            # Facades (gestión de estado)
├── ports/                  # Interfaces de API
├── infrastructure/         # Adapters HTTP
└── ui/                     # Componentes y páginas
```

## 🔧 Configuración

### Proxy API
El proxy está configurado en `proxy.conf.json` para redirigir las peticiones `/api/*` al backend en `http://localhost:5000`.

### Rutas Disponibles
- `/tasks` - Gestión de tareas
- `/invoices` - Gestión de facturas
- `/` - Redirige a `/tasks`

## ⚠️ Notas Importantes

1. **Backend requerido**: El frontend necesita que el backend esté corriendo en `http://localhost:5000` para funcionar completamente.

2. **Primera ejecución**: Si es la primera vez, ejecuta `npm install` en el directorio `apps/fresiaflow-web`.

3. **Errores de compilación**: Si hay errores, verifica que todas las rutas de importación sean correctas.

## 🐛 Solución de Problemas

### Error: "Cannot find module"
- Verifica que `node_modules` esté instalado: `npm install`
- Verifica las rutas de importación en los archivos TypeScript

### Error: "Port 4200 already in use"
- Cambia el puerto en `angular.json` o cierra el proceso que usa el puerto 4200

### El servidor no arranca
- Verifica que Node.js y npm estén instalados
- Ejecuta `npm install` nuevamente
- Revisa los logs en la consola

