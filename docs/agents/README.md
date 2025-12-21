# Sistema de Agentes FresiaFlow

Este directorio contiene la especificación completa de cada agente del sistema.

## Agentes Disponibles

| Código | Nombre | Archivo | Responsabilidad |
|--------|--------|---------|-----------------|
| ARQ | Arquitecto Hexagonal | [ARQ.md](ARQ.md) | Arquitectura, estructura, dependencias |
| DOM | Experto Dominio | [DOM.md](DOM.md) | Facturación, contabilidad, reglas de negocio |
| INT | Ingeniero Integraciones | [INT.md](INT.md) | APIs externas, resiliencia, seguridad |
| IA | Especialista IA | [IA.md](IA.md) | OpenAI, RAG, prompts, extracción |
| REV | Code Reviewer | [REV.md](REV.md) | Revisión de código, calidad |
| COG | Optimizador Cognitivo | [COG.md](COG.md) | Desbloques, planificación, claridad |
| PO | Product Owner | [PO.md](PO.md) | Priorización, MVP, valor de negocio |

## Uso

El sistema de routing está configurado en `.cursor/rules.md` y se carga automáticamente en cada sesión.

## Flujo de Trabajo Recomendado

```
PO → DOM → ARQ → INT → IA → Implementación → REV
```

1. **PO**: Define qué hacer y por qué
2. **DOM**: Valida el modelo de dominio
3. **ARQ**: Diseña la estructura técnica
4. **INT**: Especifica integraciones
5. **IA**: Configura IA si aplica
6. **Implementación**: Código
7. **REV**: Revisión final

