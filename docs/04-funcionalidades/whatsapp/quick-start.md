# 🚀 Inicio Rápido - WhatsApp Notifications

Si quieres configurar WhatsApp en **5 minutos** para pruebas rápidas, sigue estos pasos:

## Opción Rápida: Usando Token Temporal (24h)

### 1. Crear App en Meta (2 minutos)
1. Ve a [https://developers.facebook.com/apps/create/](https://developers.facebook.com/apps/create/)
2. Selecciona **"Business"** → Siguiente
3. Nombre: `FresiaFlow Test`
4. Email: tu email
5. **Crear app**

### 2. Configurar WhatsApp (1 minuto)
1. En el dashboard, busca **WhatsApp** y haz clic en **"Set up"**
2. En **"API Setup"**:
   - Copia el **Phone Number ID** (bajo el número de teléfono)
   - Copia el **Temporary access token**
3. En **"To"**: Agrega tu número personal y verifica con el código

### 3. Configurar FresiaFlow (1 minuto)
Edita `src/FresiaFlow.Api/appsettings.json`:

```json
{
  "WhatsApp": {
    "Enabled": true,
    "PhoneNumberId": "PEGA_AQUI_EL_PHONE_NUMBER_ID",
    "AccessToken": "PEGA_AQUI_EL_ACCESS_TOKEN",
    "RecipientPhone": "56912345678",
    "SendOnTaskCreation": true
  }
}
```

**Reemplaza:**
- `PEGA_AQUI_EL_PHONE_NUMBER_ID` con el Phone Number ID
- `PEGA_AQUI_EL_ACCESS_TOKEN` con el Temporary access token
- `56912345678` con tu número (código país + número, sin + ni espacios)

### 4. Probar (1 minuto)
```bash
# Iniciar API
cd src/FresiaFlow.Api
dotnet run

# En otro terminal, probar:
curl -X POST http://localhost:5000/api/whatsapp/test \
  -H "Content-Type: application/json" \
  -d '{}'
```

**¡Deberías recibir un WhatsApp!** 🎉

---

## ⚠️ Nota Importante
El **Temporary access token expira en 24 horas**. Para producción, sigue la [Guía Completa](./WHATSAPP_SETUP.md) para crear un token permanente.

---

## 📱 Uso

### Crear una tarea (envía WhatsApp automáticamente)
```bash
curl -X POST http://localhost:5000/api/tasks \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Revisar factura pendiente",
    "description": "Factura de proveedor XYZ",
    "priority": 2
  }'
```

### Enviar resumen de tareas
```bash
curl -X POST http://localhost:5000/api/whatsapp/send-tasks-summary
```

---

## ✅ Funciona Perfectamente Con

- ✅ **Creación de tareas**: Notificación instantánea
- ✅ **Facturas con baja confianza**: Se crea tarea y envía WhatsApp
- ✅ **Proveedores desconocidos**: Notificación automática
- ✅ **Sincronización OneDrive**: Si hay errores, recibes notificación

---

## 🔄 Renovar Token Temporal

Cuando expire (24h), simplemente:
1. Ve a la página de **API Setup** en Meta
2. Haz clic en **"Generate"** junto al Temporary access token
3. Copia el nuevo token
4. Actualiza `appsettings.json`
5. Reinicia la API

---

## 📚 Siguiente Paso

Para **producción** con token permanente que no expire:
👉 Lee la [Guía Completa de Configuración](./WHATSAPP_SETUP.md)

---

¡Disfruta tus notificaciones de WhatsApp! 📱✨

