# Propuesta de Estructura de Documentación

## 📋 Estructura Propuesta

```
docs/
├── README.md                          # Índice general
│
├── 01-usuarios/                      # 📖 Para usuarios finales
│   ├── README.md
│   ├── guia-usuario.md
│   └── inicio-rapido.md
│
├── 02-setup/                         # ⚙️ Configuración e instalación
│   ├── README.md
│   ├── database-setup.md
│   ├── onedrive-setup.md
│   └── whatsapp-setup.md
│
├── 03-api/                           # 🔌 Documentación de APIs
│   ├── README.md
│   └── dashboard-api.md
│
├── 04-funcionalidades/                # 🎯 Documentación por funcionalidad
│   ├── README.md
│   ├── facturas/
│   │   ├── sincronizacion.md
│   │   ├── auditoria.md
│   │   └── correcciones.md
│   └── whatsapp/
│       ├── features.md
│       ├── quick-start.md
│       └── setup.md
│
├── 05-desarrollo/                     # 💻 Para desarrolladores
│   ├── README.md
│   ├── arquitectura.md
│   └── code-analysis.md
│
└── 06-agentes/                        # 🤖 Documentación de agentes IA
    ├── README.md
    └── [archivos de agentes]
```

## 🎯 Ventajas de esta estructura

1. **Prefijos numéricos**: Fácil navegación y orden lógico
2. **Nombres descriptivos**: Fácil de encontrar por nombre
3. **Agrupación lógica**: Por audiencia y tipo de contenido
4. **Escalable**: Fácil agregar nuevas secciones

## 📝 Nomenclatura

- **Carpetas**: `NN-categoria/` (prefijo numérico + nombre descriptivo)
- **Archivos**: `nombre-descriptivo.md` (kebab-case, descriptivo)
- **README.md**: En cada carpeta para índice de esa sección

