# CI/CD: deploy automático a Azure (App Service + Azure SQL)

Guía para publicar la API en **Azure App Service** y la base en **Azure SQL Database**, y para que cada push (o merge de un PR) a `master` realice automáticamente el build, la actualización de la base con migraciones EF Core y el deploy con **GitHub Actions**.

| Pieza | Servicio de Azure | Plan gratuito |
|---|---|---|
| Contenedor lógico de todo | Resource Group | Gratis siempre |
| API (.NET 10) | App Service (Linux) | Plan **F1 Free** |
| Base de datos | Azure SQL Database | **Free offer** (100.000 vCore-segundos + 32 GB por mes) |
| Pipeline | GitHub Actions | Gratis en repos públicos (2.000 min/mes en privados) |

```
 git push master ──► GitHub Actions ──┬─► dotnet ef database update ──► Azure SQL Database
                                      └─► azure/webapps-deploy ───────► Azure App Service
```

---

## 1. Resource Group

1. En [portal.azure.com](https://portal.azure.com) → **Resource groups** → **Create**.
2. **Resource group:** `rg-legalmanager`.
3. **Region:** `East US 2` (o la región con disponibilidad de planes gratuitos).
4. **Review + create** → **Create**.

---

## 2. App Service (la API)

1. **App Services** → **Create** → **Web App**.
2. **Basics:**
   - **Resource group:** `rg-legalmanager`.
   - **Name:** `legalmanager-api` (o un nombre disponible si está tomado).
   - **Publish:** `Code`.
   - **Runtime stack:** `.NET 10 (LTS)`.
   - **Operating System:** `Linux`.
   - **Region:** la misma del Resource Group.
   - **Pricing plan:** **Free F1**.
3. **Deployment:** Continuo en **Disable** (lo gestiona GitHub Actions).
4. **Habilitar credenciales de publicación:**
   - En la Web App → **Settings → Configuration → General settings** (o en la nueva pestaña *Configuration*).
   - **SCM Basic Auth Publishing Credentials:** `On` → **Save**.

> Si el nombre del App Service es distinto a `legalmanager-api`, actualizalo en `.github/workflows/deploy.yml` → `env.AZURE_WEBAPP_NAME`.

---

## 3. Azure SQL Database (la base de datos)

1. **Azure SQL** → **Create** → **SQL databases / Single database** → **Create**.
2. Aplicar la oferta gratuita si está disponible (**Apply offer**).
3. **Basics:**
   - **Database name:** `LegalManagerDb`.
   - **Server:** Crear nuevo (ej. `sql-legalmanager`), elegir **SQL authentication** y definir admin y contraseña.
4. **Networking:**
   - **Connectivity method:** `Public endpoint`.
   - **Allow Azure services and resources to access this server:** `Yes` (necesario para el App Service y GitHub Actions).
   - **Add current client IP address:** `Yes` (para conectar desde tu PC local con SSMS/Azure Data Studio).

### Cadena de conexión para Azure:
```
Server=tcp:sql-legalmanager.database.windows.net,1433;Initial Catalog=LegalManagerDb;Persist Security Info=False;User ID=<admin>;Password=<password>;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;
```

---

## 4. Configurar variables de entorno en Azure (App Settings)

En la Web App → **Settings → Environment variables**:

| Nombre | Valor | Tipo |
|---|---|---|
| `ConnectionStrings__LegalManagerDb` | Cadena de conexión de Azure SQL | App setting / Connection string (SQLAzure) |
| `Jwt__Key` | Clave secreta segura para producción (mínimo 32 caracteres) | App setting |
| `SeedAdmin__Email` | Email del admin inicial de producción | App setting |
| `SeedAdmin__Password` | Contraseña segura del admin de producción | App setting |
| `Llm__ApiKey` | Clave privada del proveedor LLM (OpenCode Go) | App setting |

---

## 5. Secretos en GitHub (Actions)

En tu repositorio de GitHub → **Settings → Secrets and variables → Actions → New repository secret**:

| Secret | Descripción / Origen |
|---|---|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | En la Web App de Azure → **Download publish profile**. Pegar el contenido XML completo. |
| `DB_CONNECTION_STRING` | La cadena de conexión de Azure SQL con el usuario y contraseña reales. |

---

## 6. Pipeline (`.github/workflows/deploy.yml`)

* **En Pull Request:** Solo compila (`dotnet restore` + `dotnet build Release`) para asegurar que el PR no rompe nada.
* **En Push / Merge a `master`:**
  1. Compila y empaqueta la aplicación (`dotnet publish`).
  2. Aplica migraciones incrementales en Azure SQL con `dotnet ef database update --connection "$DB_CONNECTION_STRING"`.
  3. Despliega la API en Azure App Service usando `azure/webapps-deploy@v3`.
