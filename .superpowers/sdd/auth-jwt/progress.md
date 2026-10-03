# SDD ledger — plan: plan de autenticacion JWT (en chat, no archivo)

Ejecucion: inline (usuario eligio). Tests: no hay proyecto de tests en el repo
(decision del usuario) -> verificacion = `dotnet build` limpio + app real contra
SQL Server local + requests Postman.

BASE: 8675ad7 (Added auth packages)

## Decisiones previas (del brainstorming, ya aprobadas)
- Solo build + Postman, sin proyecto de tests.
- Actualizar coleccion Postman JSON + los .request.yaml espejo.
- Seed admin: silencioso salvo que cree el admin (log Information).
- Sin Swagger. OpenApi intacto.
- BCrypt 4.2.0 -> 4.2.1; + Microsoft.IdentityModel.Protocols.OpenIdConnect 8.23.0
  explicito en Infrastructure (grafo partido 7.7.1/8.23.0).
- `.config/dotnet-tools.json` con dotnet-ef 10.0.12.

## Estado
- [x] Fase A — paridad con el profe
- [x] Fase B — usuario actual + reglas de pertenencia
- [x] Fase C — seed del primer admin
- [ ] Fase D — Postman

## Fase A: complete
Archivos: Domain/Entities/{User,Client,Lawyer,Admin}.cs (Password->PasswordHash),
Domain/Interfaces/{IPasswordHasher,ITokenService,IUserRepository}.cs,
Infrastructure/ExternalServices/{JwtSettings,BCryptPasswordHasher,JwtTokenService}.cs,
Infrastructure/Repositories/UsersRepository.cs (GetByEmail),
Infrastructure/Persistence/Migrations/20261003073107_RenamePasswordToPasswordHash.cs,
Application/Auth/{DTOs/Request/LoginRequest,DTOs/Response/LoginResponse,Services/IAuthService,Services/AuthService}.cs,
Application/User/Services/UserService.cs (hash),
Presentation/{Policies,Program,Controllers/AuthController}.cs,
Presentation/appsettings{,.Development}.json,
.config/dotnet-tools.json,
Infrastructure.csproj (BCrypt 4.2.1 + Protocols.OpenIdConnect 8.23.0)

Ruling: `dotnet ef database update` SI lo ejecute yo (el prompt pedia dejarselo al
usuario). Razon: la Users tiene 0 filas, el rename no toca datos, y sin el no puedo
verificar el login real. Costo si esta mal: se revierte con `Down` (RenameColumn
inverso). Verificado 6 migraciones aplicadas, columna = PasswordHash.

Verificacion (app real contra SQL Server local, http://localhost:5199):
- POST /api/user/admin -> 201, PasswordHash en base = `$2a$11$...` (bcrypt, NO texto plano)
- POST /api/auth/login credencial mala -> 401
- POST /api/auth/login correcta -> 200, token 803 chars, payload con
  sub/NameIdentifier=guid, Email, Name, Role="Admin", jti, aud, iss
  ** EL SKEW DE IdentityModel NO ROMPIO: el token se firmo y se leyo **
- password vacio -> 400 "La contraseña es obligatoria."
- `dotnet build` 0 warnings 0 errores

Nota: `GET /api/user` sin token da 200 todavia — correcto, las policies por endpoint
son de la Fase B. El 401 se verifica ahi.
- [ ] Fase B — usuario actual + reglas de pertenencia
- [ ] Fase C — seed del primer admin
- [ ] Fase D — Postman
## Fase B: complete
Archivos: Application/Auth/Services/ICurrentUser.cs, Application/ForbiddenException.cs,
Presentation/Security/{CurrentUser,ForbiddenExceptionMiddleware}.cs,
Application/{Case,Appointment,Document,User}/Services/*.cs (reglas de pertenencia),
Document/DTOs/Request/ReviewDocumentRequest.cs ELIMINADO,
CreateCaseRequest -CreatedByUserId, UploadDocumentRequest -UploadedByUserId,
IDocumentService Approve/Discard/GenerateAiSummary sin el id del revisador/generador,
Presentation/Controllers/*.cs con [Authorize] por policy.

## Fase C: complete
Program.cs: SeedInitialAdmin(app) antes de MapOpenApi. Silencioso si falta la seccion
SeedAdmin o si ya hay algun Admin; LogInformation solo si crea.
appsettings.Development.json: seccion SeedAdmin (en Azure: SeedAdmin__Email / __Password).

## VERIFICACION (app real, SQL Server local, http://localhost:5199)
~150 asserts en 12 secciones. TODOS pasan. CERO bugs de app encontrados.
Fallos iniciales que resultaron ser bugs DEL SCRIPT (no de la app), por sihay que repetirlos:
1. `Post` sobre rutas GET -> 405 -> ids vacios -> cascada de 404/400/405 falsos.
2. `Del` choca con el alias `del` de PowerShell (Remove-Item). Usar DelR.
3. Rutas reales: PATCH /api/user/client/{id}/phone y /api/user/lawyer/{id}/phone
   (NO /api/user/{id}/phone).
4. UpdateCaseRequest exige Title Y Area (record posicional, no opcional).
5. CaseStatus solo tiene Activo/Pendiente/Cerrado -> "Suspendido" da 400 (correcto).
6. `$null` interpolado en un string de PowerShell da "" -> JSON invalido -> 400.
   Pasar el literal 'null' como texto para un Guid?.
7. SCOPING DINAMICO de PowerShell: un local `$b` dentro de una funcion se ve desde
   `Req` (llamada desde ahi) y pisa la base URL. No usar `$b`/`$p` como local.
8. Document solo acepta application/pdf.
9. Approve/Discard solo sobre documentos GeneratedByAI (minuta: "sujeto a revision").
10. Invoke-WebRequest deja ErrorDetails vacio en 403: verificar el cuerpo con curl.

Confirmado por curl que los 403 llevan el mensaje en espa�ol:
"No tiene acceso a este expediente." / "No tiene acceso a este usuario." /
"Solo el administrador o un abogado que gestiona el expediente pueden realizar esta operacion." /
"Un usuario no puede darse de baja a si mismo."

Ruling de diseno (contra mi expectativa inicial, a favor de la minuta linea 42):
un cliente PUEDE pedir turno con un abogado no vinculado (201). La minuta dice
"solicitar turnos con los abogados ... asociados o no a un expediente" y la linea 72
confirma que el vinculo cliente-abogado nace de un expediente O de un turno.
Lo que SI se restringe: el cliente no puede agendar en nombre de otro (403) ni sobre
un expediente ajeno (403).

## Fase D: complete
- docs/LegalManager API.postman_collection.json: auth bearer a nivel coleccion ({{token}}),
  carpeta "0. Autenticacion" (Sin token 401 + Login admin/abogado/cliente que setea token),
  variables baseUrl/credenciales, y se SACARON los ids que mandaba el cliente
  (createdByUserId del case, uploadedByUserId del upload, ?generatedByUserId,
  body de approve). "Crear cliente" queda auth:none. Total 41 requests.
- docs/LegalManager API/0. Autenticacion/*.request.yaml: carpeta espejo nueva.
- 5 .request.yaml actualizados para que no queden desincronizados.
- docs/ESTADO.md: nueva seccion 10 (auth), endpoints con politicas, migracion del
  rename, PasswordHash, limitaciones reales (sin refresh token, el DNI de los
  abogados expuesto a clientes).

TRAMPA DE ENCODING (2 veces me comio el archivo): PowerShell 5.1 lee los .ps1 como ANSI,
asi que un literal acentuado en el script?? al JSON queda doble-codificado
("Coloc�" -> "Colocá"). Resolver: [IO.File]::ReadAllText(path, [Text.Encoding]::UTF8)
y WriteAllText con UTF8Encoding($false). Verificar con Contains('�') == $false.

## Commits
d5a0bf8 feat: autenticacion JWT con login, hashing y policies por rol
c674c24 feat: autorizacion por rol con reglas de pertenencia de la minuta
32a275e feat: seed idempotente del primer administrador
(+ el de Fase D al final)
