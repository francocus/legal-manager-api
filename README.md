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
