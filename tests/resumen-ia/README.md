# Pruebas Postman — Resumen IA (21/10/2026)

Evidencia de la corrida de pruebas del flujo de resumen IA contra la API local (`http://localhost:5133`, SQL Server LocalDB). Los requests formales viven en `docs/LegalManager API.postman_collection.json`; acá quedan guardados los resultados reales de la corrida.

Entorno: rama `feat/apis-externas`, modelo `deepseek-v4.1-flash` (OpenCode Go, razonamiento medio), ApiKey por user-secrets.

## Resultados (camino feliz)

| # | Prueba | Request | Resultado | Qué valida |
|---|--------|---------|-----------|------------|
| 1 | Login admin | `POST /api/auth/login` | 200 | JWT emitido con la seed automática |
| 2 | Crear cliente | `POST /api/user/client` | 201 | Alta de usuario con token admin |
| 3 | Crear abogado | `POST /api/user/lawyer` | 201 | Alta con matrícula y especialidades |
| 4 | Crear expediente | `POST /api/case` | 201 | Expediente activo vinculado |
| 5 | Generar resumen IA | `POST /api/document/case/{id}/generate-summary` | **201 en 6.4s** | Llamada real al LLM end-to-end: Polly sin intervenir, contexto compuesto solo con datos registrados, documento creado como borrador (`isPendingReview: true`) |
| 6 | Listar documentos | `GET /api/document/case/{id}` | 200 | El borrador aparece en el expediente |
| 7 | Aprobar resumen | `PATCH /api/document/{id}/approve` | 200 | Regla de minuta: borrador hasta aprobación del abogado |

## Resultados (BaseUrl caída, camino de resiliencia — corrida con defecto detectado)

Se levantó la API con `Llm__BaseUrl=http://localhost:9/` (puerto muerto) para provocar fallos de conexión:

| # | Llamada | Resultado | Tiempo | Interpretación |
|---|---------|-----------|--------|----------------|
| 1 | generate-summary | 502 | 30.5s | Reintentos: 4 intentos con esperas 2s/4s/8s + conexiones rechazadas; al agotarse, la excepción de negocio responde 502 ✓ |
| 2 | generate-summary | 500 (crudo) | 6.1s | Defecto: en el intento siguiente el breaker abrió y Polly lanzó `BrokenCircuitException`, que el cliente no mapeaba: stack trace crudo |
| 3 | generate-summary | 500 (crudo) | 0.01s | Breaker abierto: falla instantánea pero sin mapear a 502 |

**Defecto y fix:** se agregó en `OpenCodeAiSummaryService` el catch de `BrokenCircuitException` que lo traduce a `AiSummaryUnavailableException` (502 con mensaje claro). Corrida posterior al fix registrada abajo.

## Resultados (BaseUrl caída, corrida post-fix)

Misma prueba, con el fix aplicado:

| # | Llamada | Resultado | Tiempo | Interpretación |
|---|---------|-----------|--------|----------------|
| 1 | generate-summary | 502 | 30.5s | Los 4 intentos agotan los reintentos; mensaje de negocio claro (evidencia: `evidence/fail2_1.json`) |
| 2 | generate-summary | 502 | 6.1s | El breaker abrió en este llamado; `BrokenCircuitException` ya mapeada a 502 |
| 3 | generate-summary | 502 | 0.007s | Breaker abierto: falla instantánea con mensaje legible, sin stack trace |

Conclusión: retry con backoff funcionando (llamada 1), breaker abriendo tras las fallas (llamada 2) y falla rápida degradada con 502 claro (llamada 3). El endpoint nunca deja un documento borrador a medias.

## Reproducción

1. `sqllocaldb start MSSQLLocalDB`
2. En el shell: `ConnectionStrings__LegalManagerDb="Server=(localdb)\\MSSQLLocalDB;Database=LegalManager;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"` y, para el camino de fallo, además `Llm__BaseUrl="http://localhost:9/"`
3. `dotnet run --project LegalManager.Presentation --no-build`
4. Ejecutar los requests de `docs/LegalManager API.postman_collection.json` en orden (login → crear cliente → crear abogado → crear expediente → generate-summary).
