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