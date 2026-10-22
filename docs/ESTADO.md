# Estado del Sistema — LegalManager API

**Última actualización:** 22/10/2026  
**Rama activa:** `master`  
**Último commit:** `a16cd57` (chore(config): limpiar variables de appsettings base, ampliar .gitignore y salvaguardar arranque)

---

## Resumen General

El backend de la API está prácticamente terminado y funcional. Todos los módulos principales están implementados con autenticación JWT, autorización por rol y reglas de negocio completas según la minuta.

---

## Estado por Módulo

### ✅ Módulo de Autenticación (`Auth`)
- Login con JWT (`POST /api/auth/login`)
- Hashing de contraseñas con BCrypt
- Tokens con claims: `sub`, `nameidentifier`, `email`, `name`, `role`, `jti`
- Expiración de 15 minutos (`ClockSkew: Zero`)
- Revocación efectiva en `OnTokenValidated`: si un usuario es dado de baja, su token queda rechazado de inmediato sin necesidad de refresh token
- Seed idempotente del primer Administrador al iniciar la aplicación

### ✅ Módulo de Usuarios (`User`)
- CRUD completo con baja lógica (`Active = false`, nunca se borra físicamente)
- Separación de endpoints por tipo: `POST /api/user/client`, `/lawyer`, `/admin`
- Solo los clientes se autorregistran (`[AllowAnonymous]` únicamente en `POST /client`)
- Abogados y Administradores solo creados por Admin
- Validación de unicidad de Email y DNI al crear y actualizar
- Actualización granular de datos de contacto (`PATCH phone`, `PATCH address`, `PATCH bar-number`, `PATCH specialties`)
- Protección de datos privados (DNI y Email solo visibles para el propio usuario o el Admin)
- Un usuario no puede darse de baja a sí mismo
- Un usuario con expedientes o turnos activos no puede ser dado de baja

### ✅ Módulo de Expedientes (`Case`)
- CRUD completo con baja lógica
- Vinculación obligatoria a un Cliente y al menos un Abogado
- Agregar/quitar abogados responsables (`PATCH /api/case/{id}/lawyers/add`, `/lawyers/remove`)
- No se puede quitar el último abogado responsable
- Cambio de estado (`activo`, `pendiente`, `cerrado`) con registro de fecha de cierre
- Un expediente cerrado no puede modificarse ni recibir documentos
- Un expediente no puede darse de baja si tiene turnos activos
- Número de expediente único en el sistema
- Filtrado por rol: Admin ve todo, Abogado solo los suyos, Cliente solo los propios

### ✅ Módulo de Turnos (`Appointment`)
- CRUD completo con baja lógica
- Confirmación, cancelación y reprogramación
- Consulta de disponibilidad horaria de un abogado (`GET /api/appointment/availability`)
- Estado calculado `EffectiveStatus`: un turno confirmado con fecha pasada se considera "Finalizado" sin persistirlo
- Protección de conflicto de agenda: validación en memoria + índice único en BD (`UX_Appointments_Lawyer_Slot`) para evitar race conditions
- Asociación opcional a un expediente (0..1)

### ✅ Módulo de Documentos (`Document`)
- Subida de archivos PDF reales (hasta 10 MB) vinculados a un expediente
- Almacenamiento físico en disco del servidor
- Listado y descarga de documentos
- Baja lógica de documentos
- No se pueden adjuntar documentos a un expediente cerrado
- Generación de resumen por IA (`POST /api/document/case/{id}/generate-summary`)
  - Conectado a un proveedor real: OpenCode Go (API compatible OpenAI), modelo `deepseek-v4.1-flash` con razonamiento medio, protegido con Polly (retry con backoff exponencial + circuit breaker + timeout por intento). Si el proveedor falla, la API responde 502 con un error claro y no deja borradores a medias
  - Los documentos generados por IA quedan como borrador (`IsPendingReview = true`) hasta que un abogado los aprueba o descarta
  - Los borradores de IA son invisibles para el cliente (retornan 404 silencioso)
- Aprobación y descarte de documentos IA por un abogado

---

## Arquitectura y Técnico

### Capas (Clean Architecture)
| Capa | Proyecto | Responsabilidad |
|------|----------|----------------|
| Domain | `LegalManager.Domain` | Entidades, interfaces de repositorio, enums, `FieldLengths` |
| Application | `LegalManager.Application` | Servicios, DTOs (por feature folder), `ICurrentUser`, `ForbiddenException` |
| Infrastructure | `LegalManager.Infrastructure` | DbContext, Repositorios, JWT, BCrypt |
| Presentation | `LegalManager.Presentation` | Controllers, Policies, CurrentUser impl, Middleware, Program.cs |

### Base de Datos
- **ORM:** Entity Framework Core (Code-First)
- **BD:** Microsoft SQL Server
- **Estrategia de herencia:** **TPC (Table-Per-Concrete-Type)** — cada tipo concreto tiene su propia tabla completa con todas las columnas heredadas. No existe tabla `Users`.
  - Tabla `Clients`: Id, FirstName, LastName, Dni, Email, PasswordHash, RegistrationDate, Active, Phone, Address
  - Tabla `Lawyers`: Id, FirstName, LastName, Dni, Email, PasswordHash, RegistrationDate, Active, Phone, BarNumber, Specialties
  - Tabla `Admins`: Id, FirstName, LastName, Dni, Email, PasswordHash, RegistrationDate, Active
- Tabla de relación N:M: `CaseLawyer` (CaseId, LawyerId)
- Índice único filtrado: `UX_Appointments_Lawyer_Slot` (LawyerId, Date, Time) para turnos activos no cancelados

### Seguridad y Autorización
- JWT Bearer con `ClockSkew = TimeSpan.Zero`
- Revocación efectiva en `OnTokenValidated` (consulta la BD en cada request)
- 3 políticas en `Policies.cs`:
  - `AdminsOnly` → solo rol `Admin`
  - `AdminOrLawyer` → roles `Admin` o `Lawyer`
  - `AllRoles` → cualquier usuario autenticado
- Autorización por contexto (ownership) dentro de la capa Application usando `ICurrentUser`

### Convenciones de Código
- **Primary Constructors** (C# 12) en todos los controladores, servicios, repositorios y servicios de infraestructura
- **Entidades ricas**: la lógica de negocio vive en las propias entidades (validaciones en constructores, métodos de comportamiento)
- **Baja lógica universal**: propiedad `Active` en todas las entidades, nunca `DELETE` físico
- **Serialización de enums como string** (`JsonStringEnumConverter`)
- Límites de columna centralizados en `Domain.FieldLengths`
- **Comentarios del código en inglés**: documentan decisiones y su motivo, no narran pasos (elección 22/10 tras el review del PR de apis-externas)

---

## Qué falta / Próximos pasos

| Tarea | Prioridad | Notas |
|-------|-----------|-------|
| Migrar a código asíncrono | ✅ Completada | Migrado a async end-to-end (commit `741ba24`): Repositorios EF Core (`ToListAsync`, `SaveChangesAsync`), Servicios de Aplicación y Controllers con `Task` y propagación de `CancellationToken`. |
| Generación de constancias por plantilla (QuestPDF) | Media | Mencionado en la minuta como funcionalidad de "generación automática" |
| Frontend (React 19) | Alta | Conectar con esta API. Ya existe desde la iteración anterior |
| Despliegue en Azure (App Service + SQL + Storage) | Baja | Para producción/entrega final |
| Tests unitarios | Media | No hay tests por el momento |

---

## Integración con APIs externas (asignación 13/10/2026)

Asignación por Franco (WhatsApp 13/10): Agus se encarga de "las APIs externas y lo de async" en la API de LegalManager.

Contexto de la cursada: comunicarse desde la API con otras APIs para utilizar sus servicios, y establecer diferentes patrones y estrategias de comunicación para reaccionar ante escenarios de inestabilidad en las APIs externas.

### Alcance mapeado

| # | Integración | Qué es | Estado |
|---|-------------|--------|--------|
| 1 | API de LLM (resumen por IA) | Llamada al proveedor elegido con los datos del expediente; devuelve el texto del resumen narrativo | Única API externa declarada explícitamente en la minuta y el TIF; es el caso central de comunicación API→API del proyecto | 
| 2 | Azure Storage / blobs | Almacenamiento de los PDFs en la nube | ⚠️ Contradicción por resolver: la minuta dice "Documentos en Azure Storage Account", el TIF dice "almacenamiento en el sistema de archivos del servidor" |
| 3 | Azure SQL vía EF Core | I/O de red hacia la base de datos | Cubierto por la migración a código asíncrono |

No llevan API externa: las constancias y plantillas (QuestPDF local, sin intervención de IA según el TIF) y el consumo del frontend (cliente-servidor, no API a API).

### Patrones y estrategias de comunicación previstos

- Cliente HTTP tipado registrado con `IHttpClientFactory` (`AddHttpClient`).
- Resiliencia con Polly (o el paquete `Microsoft.Extensions.Http.Resilience`): Retry con backoff exponencial y jitter, Circuit Breaker, Timeout y degradación elegante (si el LLM no responde tras los reintentos, la API responde con un error claro sin dejar documentos borrador a medias).
- Async/await end-to-end: controller → servicio → repositorio → HTTP → `SaveChangesAsync`, sin bloquear threads.
- Configuración por `appsettings.json` / variables de entorno: clave del proveedor y nombre del modelo, nunca hardcodeados ni commiteados.
- El resumen se redacta únicamente a partir de los datos ya registrados en el expediente (regla de la minuta), sin información externa.

### Punto de partida e implementación
El stub original (`DocumentService.GenerateAiSummary` + `ComposeSummaryText`) fue reemplazado exitosamente por la integración real con OpenCode Go (`OpenCodeAiSummaryService`), conectándose a `deepseek-v4.1-flash` con resiliencia Polly y flujo asincrónico completo.

### Decisiones de arquitectura cerradas
1. **Proveedor de LLM:** OpenCode Go (API compatible OpenAI con `deepseek-v4.1-flash` y `reasoning_effort: medium`).
2. **Storage de PDFs:** Sistema de archivos del servidor (con ruta configurable por `DocumentStorage:RootPath`). Para Azure se podrá enchufar un adaptador de Blob Storage manteniendo la misma interfaz.

### Estado de implementación del resumen IA (21/10/2026)

Implementado en la rama `feat/apis-externas` (commits `6eaeb59`, `4c64569`, `5e828c8`, `b275e70`, `15ac783`, `a4f093d`, `843c0d5`, `190a273`):

- **Proveedor elegido:** OpenCode Go (suscripción del usuario; API compatible con OpenAI en `https://opencode.ai/inference/go/openai/v1/chat/completions`), modelo `deepseek-v4.1-flash` con `reasoning_effort: medium`. Contrato verificado en vivo contra la API (HTTP 200).
- **Requisitos del proveedor:** header `x-opencode-session` con un id de sesión estable (va en appsettings) y `User-Agent: LegalManagerApi/1.0` (OpenCode exige identificar al cliente; además los modelos free solo se usan desde la app OpenCode, por eso la clave se usa con modelos de pago).
- **Resiliencia (Polly):** retry con backoff exponencial (3 intentos totales, esperas 2s/4s) ante 5xx/408/429, errores de red y timeout por intento; circuit breaker (abre tras 5 fallas seguidas, incluye timeouts; espera 30s); timeout por intento (15s). Peor caso por request: ~51s (3 x 15s + esperas de backoff). `HttpClient.Timeout` queda infinito a propósito: cubriría toda la cadena de reintentos y los mataría; el corte por intento lo hace Polly. Envoltura de afuera hacia adentro: retry → breaker → timeout.
- **Estructura (estilo cátedra GestionComplejo2C):** puerto `IAiSummaryService` en `Domain/Interfaces`; cliente tipado `OpenCodeAiSummaryService` y opciones `LlmOptions`/`LlmResilienceOptions` en `Infrastructure/ExternalServices`; registro en `Program.cs` con `AddHttpClient<Interfaz, Impl>` + `AddPolicyHandler` (el primero agregado es el más externo).
- **Fallo integrado (tarea 8):** todo fallo del LLM (reintentos agotados, breaker abierto, timeout, error upstream tipo 402/403, ApiKey ausente) levanta `AiSummaryUnavailableException` y el endpoint responde 502 con un mensaje claro, sin crear documentos borrador a medias.
- **Async (tarea 7):** el flujo generate-summary es async end-to-end: controller → `DocumentService.GenerateAiSummaryAsync` → `SaveChangesAsync`.
- **Cuota (review 22/10):** un solo borrador de resumen IA pendiente por expediente; mientras haya uno esperando revisión, el endpoint responde 409 hasta aprobarlo o descartarlo. El chequeo corre antes de llamar al LLM.
- **Config de la clave:** `dotnet user-secrets set "Llm:ApiKey" "<key>"` (ya configurada en esta máquina; nunca se commitea; si falta, la API arranca igual y solo el endpoint de resumen responde 502 con instrucciones para setearla).
- **Demo manual (tarea 9) para la facu con Postman:** cambiar `Llm:BaseUrl` a un puerto muerto (ej. `http://localhost:9/`), levantar la API, loguearse y llamar `POST /api/document/case/{id}/generate-summary`: en logs se ven los reintentos (2s/4s) y al final un 502. Repetir la llamada 5 veces para ver el circuit breaker abrirse (falla instantánea sin esperar). Después revertir la BaseUrl y probar con la real para ver un resumen generado de verdad.

---

## Archivos clave

| Archivo | Descripción |
|---------|-------------|
| `LegalManager.Presentation/Program.cs` | Entry point: DI, JWT, policies, seed de admin |
| `LegalManager.Infrastructure/Persistence/LegalManagerDbContext.cs` | Configuración EF Core, TPC, FKs, índices |
| `LegalManager.Infrastructure/Migrations/` | Migraciones EF Core: `InitialCreate` y `AddUniqueEmailDniIndexes` |
| `LegalManager.Application/*/Services/` | Servicios con lógica de negocio asincrónica y reglas de la minuta |
| `LegalManager.Presentation/Security/CurrentUser.cs` | Lee el JWT del contexto HTTP y expone `Id`, `IsAdmin`, `IsLawyer`, `IsClient` |
| `docs/minuta.md` | Requisitos, reglas de negocio y matriz de permisos del proyecto |
| `docs/diagrama-clases.png` | Diagrama de clases conceptual |
| `docs/LegalManager API.postman_collection.json` | Colección Postman con el 100% de los endpoints, login y variables |
| `docs/tasks/apis-externas.md` | Registro de tareas y decisiones de integración de APIs externas |
| `docs/pruebas/resumen-ia/` | Evidencia y corridas de prueba de resiliencia y resumen IA |
