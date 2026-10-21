# Feature: Integración de APIs externas con resiliencia (resumen IA + async)

**Fecha:** 2026-10-13
**Asignación:** Franco → Agus (WhatsApp 13/10): "las APIs externas y lo de async".
**Contexto de cursada:** comunicación API→API y patrones/estrategias ante inestabilidad de las APIs externas.
**Referencia cátedra:** `AgustinBaezRep/GestionComplejo2C` (repo del profesor, ejemplo Open-Meteo: `IWeatherService` en `Domain/Interfaces`, cliente tipado con `AddHttpClient`, `WeatherSettings`). Su estrategia ante inestabilidad es solo el `Timeout` del HttpClient; decisión del usuario (21/10): mantener Polly como mejora (retry + circuit breaker) sobre esa base.

## Objetivo

Reemplazar el stub de `DocumentService.GenerateAiSummary` por una integración real con un proveedor de LLM, protegiendo la llamada con Polly (Retry exponencial, Circuit Breaker, Timeout) y dejando el flujo del resumen en async end-to-end.

## Decisiones pendientes

1. Proveedor de LLM: → **elegido: OpenCode Go, modelo deepseek-v4.1-flash con reasoning_effort medium (contrato verificado contra la API el 21/10).**
2. Storage de PDFs: Azure Blob vs sistema de archivos (módulo de Thiago, no de esta feature).
3. Ownership overlap: integración LLM (Agus) vs módulo documental IA/cloud (Thiago).

## Tareas

| # | Tarea | Estado |
|---|-------|--------|
| 1 | Paquetes de resiliencia Polly en Infrastructure | ✅ hecha |
| 2 | Config `Llm` en appsettings + `LlmResilienceOptions` | ✅ hecha |
| 3 | Clase estática `LlmPolicies` (retry + circuit breaker) | ✅ hecha |
| 4 | Interfaz `IAiSummaryService` en `Domain/Interfaces` + excepción de negocio | ✅ hecha |
| 5 | Registro `AddHttpClient` tipado con policies en `Program.cs` (estilo cátedra) | ✅ hecha |
| 6 | Implementación `OpenCodeAiSummaryService` en `Infrastructure/ExternalServices` | ✅ hecha |
| 7 | Async del flujo resumen (repos/servicio/controller) | ✅ hecha |
| 8 | Fallo del LLM integrado al error handling (catch 502 en el controller) | ✅ hecha |
| 9 | Verificación: build + pruebas manuales Postman (demo retry/breaker) | ✅ hecha: build 0 errores + pruebas ejecutadas (camino feliz 201 en 6.4s con LLM real; BaseUrl caída 502/502/502 con breaker). Evidencia en tests/resumen-ia/ |
| 10 | Actualizar `docs/ESTADO.md` con lo implementado | ✅ hecha |

## Restricciones y convenciones

- Políticas y cliente van en `LegalManager.Infrastructure` (patrón cátedra; la propia `ExternalServices/` ya aloja JwtTokenService y BCryptPasswordHasher).
- La interfaz del puerto va en `Domain/Interfaces` (misma convención que `IPasswordHasher`/`ITokenService` en este repo y `IWeatherService` en la cátedra); Application y Application services solo la consumen.
- ApiKey: por user-secrets / variables de entorno, nunca commiteada (mismo criterio que `Jwt:Key`).
- Serialización con System.Text.Json (convención del proyecto, no Newtonsoft).
- Retry solo sobre 5xx/408/429/errores de red, no sobre 400/401/403.
- Wrap: retry por fuera del circuit breaker (el breaker cuenta intento por intento).
- Regla de negocio (minuta): el resumen se redacta solo con datos ya registrados en el expediente; el resultado queda borador hasta aprobación del abogado.
- UI copy es otro dominio: este trabajo no toca frontend.

## Registro de commits

- `ab02e7b` docs: agregar plan de integración con APIs externas a ESTADO.md (pre-feature, en master)
- Rama de la feature: `feat/apis-externas`
- `0aa8ca0` chore: iniciar feature apis-externas con documento ODD de tareas
- `6eaeb59` build: agregar Polly y Microsoft.Extensions.Http.Polly para resiliencia (tarea 1; incluyó bump de Microsoft.Extensions.Options 10.0.12 → 10.0.13 por NU1605)
- `4c64569` feat: agregar configuracion Llm (OpenCode Go) con opciones de resiliencia (appsettings.json; ApiKey queda fuera, por user-secrets/env)
- `5e828c8` feat: agregar LlmResilienceOptions para bindear la config de resiliencia (tarea 2)
- `b275e70` feat: agregar politicas de retry y circuit breaker para el LLM (tarea 3)
- `15ac783` feat: configurar deepseek-v4.1-flash (razonamiento medio) y habilitar user secrets
- `a4f093d` feat: agregar puerto IAiSummaryService y excepcion de resumen IA (tarea 4)
- `843c0d5` feat: agregar cliente OpenCode del LLM con timeout por intento (tarea 6)
- `190a273` feat: conectar resumen IA con async end-to-end y error 502 (tareas 5, 7 y 8)

## Proveedor elegido

OpenCode Go (suscripción $10/mes de OpenCode): API compatible con OpenAI en `https://opencode.ai/inference/go/openai/v1/chat/completions`, auth por API key de la consola de OpenCode. Modelo default: `glm-5.3-flash` (rápido, buen español, límite $60/mes incluido); alternativas free por tiempo limitado configurables desde appsettings (minimax-m3.1-flash-free, longcat-2.5-preview-free).

Requisitos del endpoint Go: header `x-opencode-session` con un id de sesión estable (va en appsettings) y `User-Agent` propio del cliente (`LegalManagerApi/1.0`). Los modelos con sufijo `-free` solo se usan desde la app OpenCode; desde la API se usa el modelo pago `deepseek-v4.1-flash`.
