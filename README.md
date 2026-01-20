# Notion Marketplace Webhook Handler 🚀

🇬🇧 English
### Overview
Notion Marketplace Webhook Handler is a robust, production-ready ASP.NET Core Web API designed to process webhooks from the Notion Marketplace.

Unlike simple scripts, this solution handles high-concurrency scenarios using a Fire-and-Forget architecture with background task queues. It ensures that Notion always receives a `200 OK` response in milliseconds (<300ms), while heavy processing (such as sending confirmation emails via SMTP) happens asynchronously without blocking the main thread.

### ✨ Key Features
- Async Architecture: Implements IHostedService and Background Queues to prevent webhook timeouts (`Error 502/504`).
- Strict Typing: Custom Payload models (NotionPayload) to safely parse Unix Timestamps and optional fields.
- Email Automation: Integrated SMTP service (Gmail/Outlook) with HTML support and embedded signatures.
- Secure: Validates incoming payloads and handles webhook.test events gracefully.
- Docker Ready: Includes a multi-stage Dockerfile optimized for production (Coolify/Portainer friendly).

### 🛠 Tech Stack
- .NET 10 (C#)
- ASP.NET Core Web API
- Docker (Linux Alpine images)
- System.Net.Mail for SMTP

### 🚀 Getting Started
Prerequisites
- .NET 10 SDK
- Docker (Optional)

### Installation
- Clone the repository
    ``` Bash
    git clone https://github.com/lautaro-rojas/NotionMarketplaceWebhook.git
    cd NotionMarketplaceWebhook
    ```

- Configure Environment 

    Update appsettings.json or use Environment Variables (recommended for Docker):

    This project is pre-configured to work seamlessly with Google Gmail SMTP.

    Note: For Gmail, you cannot use your regular login password. You must generate an App Password.
    ``` JSON
    {
        "SMTP_FROM_NAME": "Your name",
        "SMTP_HOST": "smtp.gmail.com",
        "SMTP_PASS": "Your password",
        "SMTP_PORT": 587,
        "SMTP_USER": "Your gmail",
        "OWNER_EMAIL": "Your gmail"
    }
    ```
    #### 🔑 How to get your Google App Password (SMTP_PASS)
    To send emails via Gmail API securely, follow these steps:
    1. Go to your Google Account Security page.
    2. Crucial: Ensure 2-Step Verification is turned ON. App Passwords will not appear if this is off.
    3. In the search bar at the top, type "App passwords" and select it.
    4. Create a new app name (e.g., "Notion Webhook") and click Create.
    5. Google will generate a 16-character code (e.g., abcd efgh ijkl mnop).
    6. Copy this code and paste it into the "Password" field in your configuration. You don't need the spaces.

- Run Locally
    ``` Bash
    dotnet run
    ```

- Deploy with Docker
    ``` Bash
    docker build -t notion-webhook .
    docker run -d -p 8080:8080 --env-file .env notion-webhook
    ```

### Setup in Notion Marketplace
Once your server is running (either locally with a tunnel or on a VPS), you need to register the webhook URL in your Notion Marketplace settings.

URL Format: Your API endpoint is defined in the controller as /webhook.

Example: `https://your-public-domain.com/webhook`

Protocol: Notion requires HTTPS. Plain http:// URLs will fail with a 502 or FetchError.

Validation: When you save the URL, Notion will send a test request. If your server is configured correctly, you will see a blue checkmark ✅.

💡 Pro Tip: If you are testing locally, use a tunneling tool like ngrok or Cloudflare Tunnel to get a public HTTPS URL (e.g., `https://random-id.ngrok-free.app/webhook`).


### 🤝 Support & Sponsorship
If this project helped you monetize your Notion templates or saved you development time, please consider sponsoring my work! Your support allows me to maintain this repository and create more open-source tools for the Notion ecosystem.

❤️ Sponsor this project on GitHub

<a name="español"></a>

## 🇪🇸 Español
### Descripción General
Notion Marketplace Webhook Handler es una API Web robusta construida con ASP.NET Core, diseñada para procesar webhooks del Notion Marketplace de manera eficiente.

A diferencia de scripts simples, esta solución implementa una arquitectura de Fire-and-Forget utilizando colas de tareas en segundo plano. Esto garantiza que Notion siempre reciba una respuesta `200 OK` en milisegundos (<300ms), evitando errores de Timeout, mientras que el procesamiento pesado (como el envío de correos SMTP) se ejecuta asíncronamente sin bloquear el hilo principal.

### ✨ Características Principales
- Arquitectura Asíncrona: Implementación de IHostedService y Background Queues para evitar Timeouts (`Error 502/504`).
- Tipado Estricto: Modelos personalizados (NotionPayload) para parsear correctamente Timestamps Unix y campos opcionales.
- Automatización de Emails: Servicio SMTP integrado con soporte para HTML.
- Seguro: Validación de payloads entrantes y manejo correcto de eventos webhook.test.
- Listo para Docker: Incluye un Dockerfile multi-etapa optimizado para producción (ideal para Coolify/Portainer).

### 🛠 Tecnologías
- .NET 10 (C#)
- ASP.NET Core Web API
- Docker (Imágenes Linux Alpine)
- System.Net.Mail para SMTP

### 🚀 Comenzando
Requisitos
- .NET 10 SDK
- Docker (Opcional)

### Instalación
- Clonar el repositorio
    ``` Bash
    git clone https://github.com/lautaro-rojas/NotionMarketplaceWebhook.git
    cd NotionMarketplaceWebhook
    ```

- Configurar Entorno 

    Actualiza el appsettings.json o usa Variables de Entorno (recomendado para Docker)

    Este proyecto está pre-configurado para funcionar perfectamente con Google Gmail SMTP.

    Nota: Para Gmail, no puedes usar tu contraseña normal de inicio de sesión. Debes generar una Contraseña de Aplicación.

    ``` JSON
    {
        "SMTP_FROM_NAME": "Tu nombre",
        "SMTP_HOST": "smtp.gmail.com",
        "SMTP_PASS": "Tu contraseña",
        "SMTP_PORT": 587,
        "SMTP_USER": "Tu gmail",
        "OWNER_EMAIL": "Tu gmail"
    }
    ```
    
    #### 🔑 Cómo obtener tu Contraseña de Aplicación de Google (SMTP_PASS)
    Para enviar correos vía Gmail de forma segura, sigue estos pasos:
    1. Ve a la página de Seguridad de tu Cuenta de Google.
    2. Crucial: Asegúrate de que la Verificación en 2 pasos esté ACTIVADA. La opción de contraseñas de aplicación no aparecerá si esto está desactivado.
    3. En la barra de búsqueda superior, escribe "Contraseñas de aplicaciones" y selecciónalo.
    4. Escribe un nombre para la app (ej: "Notion Webhook") y haz clic en Crear.
    5. Google generará un código de 16 caracteres (ej: abcd efgh ijkl mnop).
    6. Copia este código y pégalo en el campo "Password" de tu configuración. No es necesario incluir los espacios.

- Ejecutar Localmente
    ``` Bash
    dotnet run
    ```

- Desplegar con Docker
    ``` Bash
    docker build -t notion-webhook .
    docker run -d -p 8080:8080 --env-file .env notion-webhook
    ```

### Configuración en Notion Marketplace
Una vez que tu servidor esté corriendo (ya sea localmente con un túnel o en un VPS), necesitas registrar la URL del webhook en la configuración del Notion Marketplace.

Formato de URL: Tu endpoint está definido en el controlador como /webhook.

Ejemplo: `https://tu-dominio-publico.com/webhook`

Protocolo: Notion requiere HTTPS obligatoriamente. Las URLs con `http://` fallarán con un error 502 o FetchError.

Validación: Al guardar la URL, Notion enviará una petición de prueba. Si tu servidor responde correctamente, verás un tilde azul de validación ✅.

💡 Consejo Pro: Si estás probando en tu máquina local (localhost), utiliza una herramienta de túnel como ngrok o Cloudflare Tunnel para obtener una URL pública HTTPS (ej: `https://random-id.ngrok-free.app/webhook`).


### 🤝 Soporte y Patrocinio
Si este proyecto te ayudó a monetizar tus plantillas de Notion o te ahorró tiempo de desarrollo, ¡considera patrocinar mi trabajo! Tu apoyo me permite mantener este repositorio y crear más herramientas open-source para el ecosistema de Notion.

❤️ Patrocinar este proyecto en GitHub