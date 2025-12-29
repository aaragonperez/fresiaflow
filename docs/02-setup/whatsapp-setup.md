# 📱 Configuración de Notificaciones por WhatsApp

Esta guía te ayudará a configurar las notificaciones de WhatsApp para FresiaFlow usando **Meta WhatsApp Business API**.

## 🎯 ¿Qué lograrás?

Recibirás notificaciones automáticas por WhatsApp cuando:
- Se cree una nueva tarea pendiente
- Haya tareas con alta prioridad
- (Opcional) Resumen diario de tareas pendientes

---

## 📋 Requisitos Previos

1. Una cuenta de Facebook Business
2. Una cuenta de Meta for Developers
3. Un número de teléfono para WhatsApp Business
4. Acceso de administrador a tu negocio en Facebook

---

## 🚀 Paso 1: Crear Aplicación en Meta for Developers

### 1.1 Accede a Meta for Developers
1. Ve a [https://developers.facebook.com/](https://developers.facebook.com/)
2. Inicia sesión con tu cuenta de Facebook
3. Haz clic en **"My Apps"** en el menú superior
4. Haz clic en **"Create App"**

### 1.2 Configura tu Aplicación
1. Selecciona **"Business"** como tipo de aplicación
2. Completa los datos:
   - **App Name**: `FresiaFlow Notifications` (o el nombre que prefieras)
   - **App Contact Email**: Tu email
   - **Business Portfolio**: Selecciona tu negocio o crea uno nuevo
3. Haz clic en **"Create App"**

### 1.3 Agrega WhatsApp Product
1. En el dashboard de tu app, busca **"WhatsApp"** en la lista de productos
2. Haz clic en **"Set up"**
3. Selecciona tu **Business Portfolio**
4. Completa la configuración inicial

---

## 🔐 Paso 2: Obtener Credenciales

### 2.1 Phone Number ID
1. En el dashboard de WhatsApp, ve a **"API Setup"**
2. En la sección **"From"**, verás tu número de teléfono
3. Haz clic en el número para expandir y copia el **Phone Number ID**
   - Ejemplo: `123456789012345`
4. **GUARDA ESTE VALOR** - lo necesitarás para la configuración

### 2.2 Access Token (Token Temporal para Pruebas)
1. En la misma página de **"API Setup"**
2. Verás un **"Temporary access token"**
3. Haz clic en **"Copy"** para copiarlo
4. **NOTA**: Este token expira en 24 horas. Para producción, necesitas crear un token permanente (Paso 3)

### 2.3 Verificar tu Número de Teléfono de Prueba
1. En **"API Setup"**, en la sección **"To"**
2. Agrega tu número de teléfono personal haciendo clic en **"Add phone number"**
3. Ingresa tu número con código de país (ejemplo: +56912345678)
4. Recibirás un código de verificación por WhatsApp
5. Ingresa el código para verificar

---

## 🔑 Paso 3: Crear Token Permanente (Para Producción)

### 3.1 Crear System User
1. Ve a **Business Settings** en Facebook Business Manager
2. En el menú lateral, selecciona **"Users"** → **"System Users"**
3. Haz clic en **"Add"**
4. Nombre: `FresiaFlow WhatsApp Service`
5. Role: **Admin**
6. Haz clic en **"Create System User"**

### 3.2 Generar Token Permanente
1. Haz clic en el System User que acabas de crear
2. Haz clic en **"Generate New Token"**
3. Selecciona tu app **"FresiaFlow Notifications"**
4. Selecciona los permisos:
   - ✅ `whatsapp_business_messaging`
   - ✅ `whatsapp_business_management`
5. Token expiration: Selecciona **"Never expire"** (60 días o más)
6. Haz clic en **"Generate Token"**
7. **COPIA Y GUARDA ESTE TOKEN** - solo se muestra una vez

### 3.3 Asignar Activos al System User
1. En la página del System User, ve a **"Assign Assets"**
2. Selecciona **"Apps"**
3. Busca tu app `FresiaFlow Notifications`
4. Marca la casilla y selecciona **"Full Control"**
5. Haz clic en **"Save Changes"**

---

## ⚙️ Paso 4: Configurar FresiaFlow

### 4.1 Editar appsettings.json
Abre el archivo `src/FresiaFlow.Api/appsettings.json` y configura la sección `WhatsApp`:

```json
{
  "WhatsApp": {
    "Enabled": true,
    "PhoneNumberId": "TU_PHONE_NUMBER_ID_AQUI",
    "AccessToken": "TU_ACCESS_TOKEN_PERMANENTE_AQUI",
    "RecipientPhone": "56912345678",
    "SendOnTaskCreation": true,
    "SendDailySummary": false,
    "DailySummaryTime": "09:00"
  }
}
```

**Parámetros:**
- `Enabled`: `true` para activar notificaciones, `false` para desactivar
- `PhoneNumberId`: El Phone Number ID que copiaste en el Paso 2.1
- `AccessToken`: El token permanente que generaste en el Paso 3.2
- `RecipientPhone`: Tu número de teléfono con código de país (sin + ni espacios)
  - ✅ Correcto: `56912345678` (Chile)
  - ❌ Incorrecto: `+56 9 1234 5678`
- `SendOnTaskCreation`: `true` para enviar notificación al crear cada tarea
- `SendDailySummary`: `true` para enviar resumen diario (próximamente)
- `DailySummaryTime`: Hora del resumen diario en formato 24h (próximamente)

### 4.2 Variables de Entorno (Recomendado para Producción)
Para mayor seguridad, usa variables de entorno en lugar de guardar el token en el archivo:

**Windows (PowerShell):**
```powershell
$env:WhatsApp__AccessToken="tu_token_aqui"
$env:WhatsApp__PhoneNumberId="tu_phone_number_id"
```

**Linux/Mac:**
```bash
export WhatsApp__AccessToken="tu_token_aqui"
export WhatsApp__PhoneNumberId="tu_phone_number_id"
```

---

## ✅ Paso 5: Probar la Configuración

### 5.1 Iniciar la API
```bash
cd src/FresiaFlow.Api
dotnet run
```

### 5.2 Verificar Estado
Abre tu navegador o Postman y ejecuta:

```http
GET http://localhost:5000/api/whatsapp/status
```

Deberías ver:
```json
{
  "isConfigured": true,
  "isEnabled": true,
  "phoneNumberId": "Configurado",
  "recipientPhone": "56912345678"
}
```

### 5.3 Enviar Mensaje de Prueba
```http
POST http://localhost:5000/api/whatsapp/test
Content-Type: application/json

{
  "recipientPhone": "56912345678"
}
```

**Si todo está bien**, deberías recibir un mensaje de WhatsApp:
> ✅ **Prueba de Conexión Exitosa**
> 
> FresiaFlow está correctamente configurado para enviar notificaciones por WhatsApp.
> 
> 🕐 23/12/2024 15:30:00

### 5.4 Crear una Tarea de Prueba
```http
POST http://localhost:5000/api/tasks
Content-Type: application/json

{
  "title": "Revisar factura de prueba",
  "description": "Esta es una tarea de prueba para WhatsApp",
  "priority": 2
}
```

Deberías recibir un WhatsApp con la notificación de la tarea.

---

## 🐛 Solución de Problemas

### Error: "No autorizado" o "Invalid access token"
- ✅ Verifica que hayas copiado el token completo (son muy largos)
- ✅ Asegúrate de usar el token permanente, no el temporal
- ✅ Verifica que el System User tenga permisos sobre la app

### Error: "Phone number not verified"
- ✅ Verifica tu número en la sección "To" del API Setup
- ✅ Asegúrate de haber ingresado el código de verificación

### Error: "Message failed to send"
- ✅ Verifica que el PhoneNumberId sea correcto
- ✅ Verifica el formato del número de teléfono (sin + ni espacios)
- ✅ Asegúrate de que el número esté verificado en Meta

### No recibo mensajes
- ✅ Verifica que `"Enabled": true` en appsettings.json
- ✅ Revisa los logs de la API para ver errores
- ✅ Verifica que el número de teléfono esté en la whitelist de Meta

---

## 📊 Características Disponibles

### ✅ Implementadas
- [x] Notificación al crear tarea
- [x] Envío manual de resumen de tareas
- [x] Mensaje de prueba
- [x] Verificación de estado

### 🔜 Próximamente
- [ ] Resumen diario automático
- [ ] Notificaciones de facturas con baja confianza
- [ ] Configuración desde el frontend
- [ ] Múltiples destinatarios

---

## 💰 Costos

Meta WhatsApp Business API tiene un **tier gratuito**:
- **Primeras 1,000 conversaciones/mes**: GRATIS
- **Después de 1,000**: Varía por país (~$0.01 USD por mensaje)

**Una conversación = ventana de 24 horas** donde puedes enviar múltiples mensajes.

Para FresiaFlow con notificaciones de tareas:
- Si recibes ~30 tareas/día = 900 notificaciones/mes = **GRATIS** ✅
- Si recibes >1000 tareas/mes, el costo sería mínimo (~$1-5 USD/mes)

---

## 🔐 Seguridad

### ⚠️ IMPORTANTE
- **NUNCA** subas tu `AccessToken` a Git
- **NUNCA** compartas tu token públicamente
- Usa variables de entorno en producción
- Rota el token regularmente (cada 60-90 días)

### Agregar al .gitignore
```gitignore
# Secrets
appsettings.Production.json
appsettings.*.json
!appsettings.json
```

---

## 📚 Recursos Adicionales

- [Meta WhatsApp Business API Docs](https://developers.facebook.com/docs/whatsapp/cloud-api)
- [Getting Started Guide](https://developers.facebook.com/docs/whatsapp/cloud-api/get-started)
- [Pricing](https://developers.facebook.com/docs/whatsapp/pricing)
- [WhatsApp Business Platform Policies](https://www.whatsapp.com/legal/business-policy)

---

## 🎉 ¡Listo!

Ya tienes configuradas las notificaciones de WhatsApp en FresiaFlow.

Cada vez que se cree una tarea pendiente de validar, recibirás una notificación instantánea en tu WhatsApp. 📱✨

---

**¿Necesitas ayuda?** Abre un issue en el repositorio o contacta al equipo de soporte.

