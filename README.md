# LegalManager

API de gestión para estudios jurídicos (expedientes, turnos, documentos y resumen IA).

## Configuración

**Desarrollo:** `appsettings.Development.json` trae el bootstrap completo (base de datos local, clave JWT de desarrollo y seed del admin `admin@legalmanager.local`). Solo falta la clave del LLM por user-secrets:

```bash
dotnet user-secrets set "Llm:ApiKey" "<clave>"
```

**Producción:** las claves y credenciales van por variables de entorno (nunca en archivos commiteados):

```bash
Jwt__Key="<clave JWT segura>"
SeedAdmin__Email="<admin inicial>"
SeedAdmin__Password="<contraseña segura>"
ConnectionStrings__LegalManagerDb="<cadena de conexión>"
Llm__ApiKey="<clave del proveedor LLM>"
```

## Base de datos

```bash
dotnet tool restore
dotnet ef database update --project LegalManager.Infrastructure --startup-project LegalManager.Presentation
```

## Verificación de email y recuperación de contraseña

Toda cuenta nueva (autoregistro de cliente y altas de abogados/clientes/admins hechas por un admin) arranca sin email confirmado y recibe un correo con un enlace de confirmación. Mientras la cuenta esté pendiente, el login responde 403: `Debés confirmar tu email antes de iniciar sesión.`

Los tokens de confirmación y restablecimiento viajan crudos en el enlace y vencen (24 h / 1 h); en la base solo queda el hash SHA-256 y cada token es de un solo uso.

### Endpoints públicos (`AuthController`)

- `POST /api/auth/confirm-email` `{ "token": "..." }` — confirma la cuenta con el token del enlace.
- `POST /api/auth/forgot-password` `{ "email": "..." }` — envía el enlace de restablecimiento (responde igual si el email no está registrado).
- `POST /api/auth/reset-password` `{ "token": "...", "newPassword": "..." }` — setea la contraseña nueva; usar el enlace también confirma el email.
- `POST /api/auth/resend-confirmation` `{ "email": "..." }` — reenvía el enlace de confirmación de una cuenta pendiente.

### Rescate de admin

- `PATCH /api/user/{id}/confirm-email` (requiere rol Admin) — confirma la cuenta por id, para cuando se pierde el acceso al email o para QA automatizado.

### Configuración

```bash
Email__FrontBaseUrl="<url del frontend>"   # origen al que apuntan los enlaces del email
Email__ConfirmationPath="/confirmar-email"
Email__ResetPath="/restablecer-contrasena"
Email__ConfirmationTokenLifetimeHours=24
Email__ResetTokenLifetimeMinutes=60
Smtp__Host="<servidor smtp>"
Smtp__Port=587
Smtp__User="<usuario>"
Smtp__Password="<contraseña>"
Smtp__From="<remitente>"
Smtp__UseSsl=true
```

**Atención:** si `Smtp:Host` no está configurado, la API no envía correos: el remitente registra el mensaje completo (incluido el enlace con el token) en el log con nivel Warning. Es el fallback de desarrollo/POC para probar el flujo sin SMTP; en producción hay que configurar `Smtp:Host`, porque sin él ningún usuario puede confirmarse salvo por el rescate de admin.
