# Análisis del código

## Núcleo de mapas
- `GREDOS.GIS.Map` orquesta el estado principal del mapa, exponiendo eventos para cambios de propiedades, datos, refrescos gráficos, selección de capas y cambios de vista. También mantiene contadores internos para identificar instancias de mapa y peticiones de renderizado y agrupa las colecciones de capas de usuario, trabajo y enlaces. 【F:GREDOS.GIS/GREDOS.GIS.Core/Map/Map.cs†L42-L143】
- El constructor inicializa el mapa con un identificador incremental, color de fondo blanco, colecciones vacías de capas y un `Viewport` con centro y resolución no definidos. Además fija el formato numérico en cultura en-US para compatibilidad con proveedores Oracle. 【F:GREDOS.GIS/GREDOS.GIS.Core/Map/Map.cs†L106-L130】
- La propiedad `SRID` actualiza la factoría de geometrías cuando cambia el sistema de referencia, recreando la factoría en caso necesario y propagando el SRID al `Viewport`, lo que mantiene coherencia entre geometría y visualización. 【F:GREDOS.GIS/GREDOS.GIS.Core/Map/Map.cs†L173-L195】

## Infraestructura de capas
- `GREDOS.GIS.Layers.LayerBase` actúa como superclase para las capas, definiendo eventos de ciclo de vida (datos, propiedades, SRID, disponibilidad, habilitación y agrupación) y metadatos como nombre y grupo. También gestiona identificadores incrementales y límites de visibilidad. 【F:GREDOS.GIS/GREDOS.GIS.Core/Layers/LayerBase.cs†L20-L107】
- Los constructores asignan un nombre por defecto, generan un identificador único, inicializan estado (disponibilidad, errores, límites de features) y crean un `Style` base, asegurando que las capas hereden configuraciones coherentes desde su creación. 【F:GREDOS.GIS/GREDOS.GIS.Core/Layers/LayerBase.cs†L111-L148】
- El método protegido `Dispose(bool disposing)` libera recursos gráficos (leyendas), anula factorías de geometría y estilos, y emite el evento `Disposed`, garantizando un cierre seguro antes de marcar el objeto como liberado. 【F:GREDOS.GIS/GREDOS.GIS.Core/Layers/LayerBase.cs†L150-L177】
