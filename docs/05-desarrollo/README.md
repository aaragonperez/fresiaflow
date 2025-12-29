# 💻 Documentación para Desarrolladores

Esta sección contiene documentación técnica para desarrolladores que trabajan en FresiaFlow.

## 📚 Documentos Disponibles

| Documento | Descripción |
|-----------|-------------|
| [**Análisis de Código**](./code-analysis.md) | Análisis detallado del código (legacy) |
| [**Propuesta: Unificación de Fuentes**](./propuesta-unificacion-fuentes.md) | Propuesta para unificar sincronización de fuentes y OneDrive |

## 🎯 Temas Cubiertos

- Arquitectura del sistema
- Estructura del código
- Patrones de diseño utilizados
- Guías de desarrollo

## 🔗 Enlaces Relacionados

- [Documentación de APIs](../03-api/README.md)
- [Agentes IA](../06-agentes/README.md)
- [Configuración](../02-setup/README.md)

## 🧠 Pipeline OCR + IA híbrido (2025-12)

1. **OCR base sin LLM**: usamos `PdfPig` para extraer texto + layout (bounding boxes por letra) y guardamos el resultado completo, el hash del fichero y la confianza en `InvoiceProcessingSnapshots`.
2. **Clasificación ligera**: un modelo económico (configurable, por defecto `gpt-4o-mini`) identifica tipo de doc, idioma y proveedor probable. El JSON bruto queda cacheado para evitar re-ejecuciones.
3. **Extracción estructurada**: la IA procesa solo el texto OCR mediante `InvoiceExtractionService`, persiste el JSON junto a versión de esquema y hash, y reutiliza la respuesta si el documento no cambia.
4. **Validación determinista**: reglas de totales, IVA e integridad temporal etiquetan el documento como `OK` o `DUDOSO` sin coste de IA. Los errores quedan persistidos.
5. **Fallback inteligente**: si la confianza del OCR < umbral o la validación falla, se lanza una segunda extracción con el modelo caro (`FallbackModel`) y se marca el snapshot para auditoría; el objetivo es mantenerlo <15 % de los casos.

Todos los pasos son idempotentes gracias al snapshot y pueden reintentarse de forma independiente sin reprocesar todo el documento.

## ✅ TODO / Próximas mejoras

- [ ] Orquestar lotes de OCR para aprovechar la vectorización GPU cuando haya múltiples facturas.
- [ ] Paralelizar clasificación y extracción en colas background para liberar al watcher de disco.
- [ ] Añadir métricas por etapa (latencia, % fallback, coste estimado) y exponerlas vía Prometheus.
- [ ] Implementar warm cache en Redis para no leer/eliminar el JSON del snapshot cuando sólo se consulta.
- [ ] Incorporar reglas contables avanzadas (retenciones múltiples, prorratas) en el validador determinista.

