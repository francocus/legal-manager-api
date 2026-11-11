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

## Documentos

Los PDFs que el sistema genera (resumen IA y constancia) se producen con **QuestPDF** (licencia Community): dejan de ser texto guardado como `.pdf` y pasan a ser archivos PDF reales, con plantilla fija y letra Segoe UI.

Todos los documentos generados siguen un contrato de **confirmación previa** (confirm-first): generar no persiste nada, el borrador vuelve al cliente y solo un guardado explícito lo persiste.

1. `POST /api/document/case/{caseId}/generate-summary` — genera el resumen con IA y devuelve un **borrador** (`GeneratedDocumentDraftResponse`: nombre, tipo, tamaño, texto y el PDF en base64). No toca storage ni base de datos.
2. `POST /api/document/case/{caseId}/generate-constancia` — genera la constancia con una plantilla fija (sin IA, sin llamar al LLM) y devuelve el mismo formato de borrador.
3. El cliente muestra el borrador y decide. Solo `POST /api/document/generated` (`{ "caseId": ..., "pdfBase64": ..., "summaryText": ..., "generatedByAi": ... }`) persiste el documento: valida que el contenido sea un PDF real de hasta 10 MB y el nombre del archivo lo decide el servidor (`ResumenIA.pdf` o `Constancia.pdf`).

Los documentos IA guardados llegan ya revisados (`Approved = true`): ya no existe la cola de borradores pendientes, así que se quitaron los endpoints `PATCH /api/document/{id}/approve` y `PATCH /api/document/{id}/discard`. La subida manual de PDFs (`POST /api/document`) sigue funcionando igual.

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

## Paginación (opcional)

Los endpoints que devuelven listados aceptan paginación opcional vía query params: `page` (mayor a 0, por defecto 1) y `pageSize` (entre 1 y 100, por defecto 20). Aplica a `GET /api/case`, `GET /api/appointment`, `GET /api/user`, `GET /api/user/clients`, `GET /api/user/lawyers` y `GET /api/user/admins`.

- **Sin `page` ni `pageSize`** la respuesta no cambia: sigue siendo el array plano de siempre (compatible con el SPA actual).
- **Con cualquiera de los dos params** la respuesta pasa a ser un sobre paginado:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "total": 57
}
```

`page` menor a 1 o `pageSize` fuera del rango 1..100 responde 400 con el mensaje de error correspondiente. Los ítems paginados vuelven ordenados por `Id` de forma estable; sin paginación el orden queda exactamente igual que antes.
