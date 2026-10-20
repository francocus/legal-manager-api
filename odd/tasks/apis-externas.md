# Feature: Integración de APIs externas con resiliencia (resumen IA + async)

**Fecha:** 2026-10-13
**Asignación:** Franco → Agus (WhatsApp 13/10): "las APIs externas y lo de async".
**Contexto de cursada:** comunicación API→API y patrones/estrategias ante inestabilidad de las APIs externas.
**Referencia cátedra:** gabrielgolzman/austral-book-champions-api @ `Feat-external-api`.

## Objetivo

Reemplazar el stub de `DocumentService.GenerateAiSummary` por una integración real con un proveedor de LLM, protegiendo la llamada con Polly (Retry exponencial, Circuit Breaker, Timeout) y dejando el flujo del resumen en async end-to-end.

## Decisiones pendientes

1. Proveedor de LLM: Gemini (tier gratuito) / OpenAI / Anthropic → **elegido: (pendiente)**.
2. Storage de PDFs: Azure Blob vs sistema de archivos (módulo de Thiago, no de esta feature).
3. Ownership overlap: integración LLM (Agus) vs módulo documental IA/cloud (Thiago).

## Tareas

| # | Tarea | Estado |
|---|-------|--------|
| 1 | Paquetes de resiliencia Polly en Infrastructure | en progreso |
| 2 | Config `Llm` en appsettings + `LlmResilienceOptions` | pendiente |
| 3 | Clase estática `LlmPolicies` (retry + circuit breaker) | pendiente |
| 4 | Interfaz `IAiSummaryClient` en Application + excepción de negocio | pendiente |
| 5 | Registro `AddHttpClient` con policies en `Program.cs` | pendiente |
| 6 | Implementación del cliente LLM en `Infrastructure/ExternalServices` | pendiente |
| 7 | Async del flujo resumen (repos/servicio/controller) | pendiente |
| 8 | Fallo del LLM integrado al middleware de errores | pendiente |
| 9 | Verificación: build + pruebas manuales Postman (demo retry/breaker) | pendiente |
| 10 | Actualizar `docs/ESTADO.md` con lo implementado | pendiente |

## Restricciones y convenciones

- Políticas y cliente van en `LegalManager.Infrastructure` (patrón cátedra; la propia `ExternalServices/` ya aloja JwtTokenService y BCryptPasswordHasher).
- La interfaz del puerto va en Application (la capa no conoce al vendor).
- ApiKey: por user-secrets / variables de entorno, nunca commiteada (mismo criterio que `Jwt:Key`).
- Serialización con System.Text.Json (convención del proyecto, no Newtonsoft).
- Retry solo sobre 5xx/408/429/errores de red, no sobre 400/401/403.
- Wrap: retry por fuera del circuit breaker (el breaker cuenta intento por intento).
- Regla de negocio (minuta): el resumen se redacta solo con datos ya registrados en el expediente; el resultado queda borador hasta aprobación del abogado.
- UI copy es otro dominio: este trabajo no toca frontend.

## Registro de commits

- `ab02e7b` docs: agregar plan de integración con APIs externas a ESTADO.md (pre-feature)
