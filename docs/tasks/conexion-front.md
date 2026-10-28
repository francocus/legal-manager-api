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
| 3 | Proxy de Vite hacia 5133 + default de `API_URL` en `/api` | client | ✅ hecha (61c3b36) |
| 4 | Login: `POST /auth/login` + decodificar JWT para armar el user + mapping de roles | client | ✅ hecha (bdbabe9) |
| 5 | `usersApi`: rutas en singular, arrays directos, create por rol (+ matrícula en alta de abogado) | client | ✅ hecha (bdbabe9) |
| 6 | `casesApi`: singular, arrays directos, resolución de nombres | client | ✅ hecha (9476d27) |
| 7 | `appointmentsApi`: singular + acciones PATCH (reschedule/confirm/cancel) | client | ✅ hecha (9476d27) |
| 8 | Endpoint público `POST /api/auth/register` (solo Client), sobre el código async | api | ✅ hecha (dc4e912) |
| 8b | Conectar `Register.jsx` al register real + adaptar `AppointmentPage` | client | ✅ hecha (register wired en bdbabe9; AppointmentPage en 5bf77f1) |
| 9 | Prueba E2E | ambos | ✅ smoke HTTP 10/10 (23/10); prueba visual del UI pendiente del usuario |

## No-goals

- No mergear la rama del API.
- No agregar CORS en el API: se usa proxy de Vite (decisión paso 2; CORS queda para cuando se decida deploy separado).
- No tocar la duración del JWT ni el mecanismo de seed.

## Evidencia

- 65a9600 `docs: corregir baseUrl del collection Postman a 5133` — dropeado en rebase (el master nuevo ya trae 5133)
- 039a37d `docs: agregar plan de conexión con el front` (rebased a 8760f02; archivo movido de `odd/tasks/` a `docs/tasks/` por la nueva estructura del repo)
- Rebase 23/10: master avanzó 7 commits (refactor async end-to-end, docs consolidados en `docs/`, collection Postman actualizado, workflow CI Azure). Rama alineada a 8760f02.
- Client (rama `feat/conexion-front` de `francocus/legal-manager-client`, commits en inglés): 61c3b36 proxy, bdbabe9 auth+users, 9476d27 cases+appointments, 5bf77f1 AppointmentPage.
- dc4e912 `feat: agregar registro público de clientes en auth` (reutiliza `CreateClientAsync`, sin lógica duplicada).

## Verificación E2E (smoke HTTP 23/10, vía proxy de Vite)

Register nuevo 201 → login role claim "Client" → `GET /case` y `/user/lawyers` 200 arrays → `GET /user` con cliente 403 (policy correcta) → token inválido 401 ProblemDetails → turno creado 201 ("Pendiente") → confirm 200 ("Confirmado") → admin: `GET /user` 200 con los 2 registrados → `POST /case` 201 ("Activo"). 10/10.

## Limitaciones conocidas (follow-ups del equipo)

1. El `fullName` del usuario logueado se muestra como el email: el JWT trae `name` = email; falta claim de nombre real en el token.
2. El toggle "Usuario activo" del alta/edición quedó sin efecto (el API no acepta `active`).
3. `mapCase` muestra solo el primer abogado de `lawyerIds[]`.
4. "Finalizado" existe en el vocabulario pero ningún endpoint lo setea.
5. En `CaseDetails` quedan como solo-lectura efectivos: caseNumber, startDate, cliente y abogado (el PUT solo acepta title/area/description/notes; el cambio de abogado existe vía `PATCH lawyers/add|remove`, no conectado al UI).
6. JWT de 15' sin refresh (decisión del PR #18, fuera de alcance).
7. Datos de smoke quedaron en la DB Verify (conn.test/conn.test2, EXP-CONN-001, turno 5/11).
