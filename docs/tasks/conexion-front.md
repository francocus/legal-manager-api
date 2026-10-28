# Feature: Conexión del backend (.NET) con el front (legal-manager-client)

**Fecha:** 2026-10-23
**Asignación:** Agus: conectar `legal-manager-api` con `legal-manager-client` (front React 19 + Vite + react-router).
**Contexto:** el front habla hoy el contrato viejo de Express (base `http://localhost:4000/api`, rutas en plural, wrappers `{users:[...]}`, login con `{token, user}`). El API real usa otro contrato (verificado en vivo 23/10).
**Referencia cátedra:** `AgustinBaezRep/GestionComplejo2C` (profesor Baez): backend .NET con JWT Bearer, login en `POST /api/auth/login`, Swagger. Sin CORS ni front propio; la conexión es alinear el contrato HTTP.

## Reglas de trabajo

- **API (este repo):** rama `feat/conexion-front`, **no se mergea por ahora**. Commits conventional en español (convención del repo).
- **Client (`C:/Programacion/legal-manager-client`):** rama `feat/conexion-front` (creada desde `main`); merge eventual. Commits conventional en inglés (convención de ese repo: `feat: initial commit...`).
- No commitear: `.env.local`, `package-lock.json` (WIP del usuario en el client).

## Contrato real del API (verificado 23/10, en vivo)

- `POST /api/auth/login` `{email, password}` → `200 {token}` **solo**. JWT de 15 min: claims `sub`=id, `emailaddress`, `name`=email, `role`="Admin"|"Lawyer"|"Client". Sin refresh ni register público.
- Rutas en **singular**: `/api/user`, `/api/case`, `/api/appointment`. Los listados devuelven **arrays directos** sin wrappers.
- User: `{id, firstName, lastName, fullName, dni, email, registrationDate, type: "admin"|"lawyer"|"client", phone, address}` (el detail no trae barNumber/specialties).
- Case: `{id, caseNumber, title, area: "Laboral", status: "Activo", startDate, lastUpdate, closingDate, description, notes, clientId, createdByUserId, lawyerIds[], active}`. `PATCH /{id}/status` body `{"status":"Pendiente"}`. `PUT /{id}` acepta solo `{title, area, description, notes}`.
- Appointment: `{id, title, date, time: "09:00:00", endTime, durationMinutes, reason, status: "Pendiente", effectiveStatus, area, location, notes, clientId, lawyerId, caseId}`. Availability → `{slots:["09:00:00",...]}`. Sin PUT ni DELETE: `PATCH reschedule|confirm|cancel`.
- 401/403 → ProblemDetails `{type, title, status}` (el `httpClient` del client ya lo lee con `title`).

## Decisiones pendientes

1. **Register:** → **decidido (23/10): la aplicación necesita un register real.** Se agrega un endpoint público en el API (`POST /api/auth/register`) que da de alta solo `Client`; `Lawyer`/`Admin` siguen creándose por alta de admin. Conectado a `Register.jsx` (tarea 8).
2. **Refresh token:** JWT vence a los 15' y no hay refresh. No se toca (decisión del PR #18, fuera de esta feature).
3. **Nombres de cliente/abogado en case/appointment:** el API manda solo IDs; se resuelve en el client con la lista de users (para que el front funcione con el API actual sin cambios de backend, dado que el client sí se va a mergear y el API no todavía).

## Tareas

| # | Tarea | Repo | Estado |
|---|-------|------|--------|
| 1 | Ramas de trabajo `feat/conexion-front` en ambos repos | ambos | ✅ hecha |
| 2 | Corregir `baseUrl` del collection Postman (5199 → 5133) | api | ✅ hecha (65a9600) |
| 3 | Proxy de Vite hacia 5133 + default de `API_URL` en `/api` | client | pendiente |
| 4 | Login: `POST /auth/login` + decodificar JWT para armar el user + mapping de roles | client | pendiente |
| 5 | `usersApi`: rutas en singular, arrays directos, create por rol | client | pendiente |
| 6 | `casesApi`: singular, arrays directos, resolución de nombres | client | pendiente |
| 7 | `appointmentsApi`: singular + acciones PATCH (reschedule/confirm/cancel) | client | pendiente |
| 8 | Endpoint público `POST /api/auth/register` (solo Client) | api | pendiente |
| 8b | Conectar `Register.jsx` al register real | client | pendiente |
| 9 | Prueba E2E manual (API + client levantados, flujo completo) | ambos | pendiente |

## No-goals

- No mergear la rama del API.
- No agregar CORS en el API: se usa proxy de Vite (decisión paso 2; CORS queda para cuando se decida deploy separado).
- No tocar la duración del JWT ni el mecanismo de seed.

## Evidencia

- 65a9600 `docs: corregir baseUrl del collection Postman a 5133`
