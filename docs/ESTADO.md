# Estado del Sistema — LegalManager API

**Última actualización:** 13/10/2026  
**Rama activa:** `auth`  
**Último commit:** `615bed7` — fix database tables (TPC)

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
- Agregar/quitar abogados responsables (`PUT /api/case/{id}/add-lawyer`, `/remove-lawyer`)
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
  - ⚠️ **TODO:** actualmente genera texto concatenando datos del expediente — falta conectar a API real de IA (Gemini, etc.)
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

---

## Qué falta / Próximos pasos

| Tarea | Prioridad | Notas |
|-------|-----------|-------|
| Conectar API de IA real para resumen de expediente | Alta | Reemplazar el stub en `DocumentService.GenerateAiSummary` con llamada a Gemini, GPT u otro LLM |
| Migrar a código asíncrono | Media | Todo usa `ToList`/`SaveChanges` síncronos. Lo idiomático es `ToListAsync`/`SaveChangesAsync` para liberar el thread del servidor mientras espera la BD (mejor escalabilidad bajo carga) |
| Generación de constancias por plantilla (QuestPDF) | Media | Mencionado en la minuta como funcionalidad de "generación automática" |
| Frontend (React 19) | Alta | Conectar con esta API. Ya existe desde la iteración anterior |
| Despliegue en Azure (App Service + SQL + Storage) | Baja | Para producción/entrega final |
| Tests unitarios | Media | No hay tests por el momento |

---

## Archivos clave

| Archivo | Descripción |
|---------|-------------|
| `LegalManager.Presentation/Program.cs` | Entry point: DI, JWT, policies, seed de admin |
| `LegalManager.Infrastructure/Persistence/LegalManagerDbContext.cs` | Configuración EF Core, TPC, FKs, índices |
| `LegalManager.Infrastructure/Migrations/` | Migración única `InitialCreate` (limpia, sin historial TPT) |
| `LegalManager.Application/*/Services/` | Servicios con toda la lógica de negocio y reglas de la minuta |
| `LegalManager.Presentation/Security/CurrentUser.cs` | Lee el JWT del contexto HTTP y expone `Id`, `IsAdmin`, `IsLawyer`, `IsClient` |
| `docs/minuta.md` | Requisitos, reglas de negocio y matriz de permisos del proyecto |
| `docs/diagrama-clases.png` | Diagrama de clases conceptual |
| `docs/LegalManager API.postman_collection.json` | Colección Postman con todos los endpoints, login y token |
