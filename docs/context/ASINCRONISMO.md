# Asincronismo de punta a punta (`async` / `await`)

Guía conceptual de por qué LegalManager API es asíncrona **en todas las capas**, qué cambió en el código y qué pasaría si no lo fuera.

| Capa | Antes | Ahora |
|---|---|---|
| Presentation (controllers) | `ActionResult<T> GetById(Guid id)` | `async Task<ActionResult<T>> GetById(Guid id, CancellationToken ct)` |
| Application (services) | `CaseResponse? GetById(Guid id)` | `Task<CaseResponse?> GetByIdAsync(Guid id, CancellationToken ct)` |
| Domain (interfaces) | `Case? GetById(Guid id)` | `Task<Case?> GetByIdAsync(Guid id, CancellationToken ct)` |
| Infrastructure (repositorios) | `.ToList()`, `.FirstOrDefault()`, `SaveChanges()` | `.ToListAsync()`, `.FirstOrDefaultAsync()`, `SaveChangesAsync()` |
| Infrastructure (API externa LLM) | ya era `async`, pero el resto del flujo era sync | flujo async end-to-end con propagación de `CancellationToken` |

---

## 1. El problema: los hilos son un recurso limitado

ASP.NET Core atiende cada request con un **hilo del *thread pool***. El pool tiene una cantidad acotada de hilos y crea nuevos lentamente cuando la carga aumenta.

Casi todo lo que hace esta API es **esperar operaciones de entrada/salida (I/O)**:
* Esperar a que SQL Server devuelva los expedientes o clientes.
* Esperar a que SQL Server confirme el `INSERT` o `UPDATE` de un turno o usuario.
* Esperar a que el proveedor de LLM (OpenCode Go) devuelva el texto del resumen narrativo.
* Esperar a que se escriba o lea un archivo PDF en el almacenamiento.

Durante esas esperas la CPU no hace trabajo de cálculo. La diferencia fundamental está en qué hace el hilo mientras espera:

### Versión sincrónica: el hilo se bloquea
```csharp
public Case? GetById(Guid id) =>
    context.Cases.FirstOrDefault(c => c.Id == id); // El hilo queda dormido esperando a SQL Server
```
El hilo queda retenido sin trabajar hasta que llega la respuesta. No puede atender a nadie más.

### Versión asíncrona: el hilo se libera
```csharp
public Task<Case?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
    context.Cases.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
```
Cuando el código llega a un `await` sobre una operación de E/S (red, disco, base de datos):
1. Se registra un callback ("cuando llegue la respuesta, continuá desde acá").
2. El hilo **vuelve al pool** y queda disponible para atender otros requests concurrentes.
3. Cuando la base de datos o la API externa responde, cualquier hilo libre del pool retoma la ejecución del método.

> **Analogía del mozo:** Un mozo (hilo) toma el pedido, lo deja en la cocina (SQL Server / LLM) y sigue atendiendo otras mesas, en vez de quedarse parado inmóvil al lado del chef hasta que el plato esté cocinado. La cocina no cocina más rápido, pero el mismo mozo atiende muchas más mesas simultáneamente.

---

## 2. Propagación de `CancellationToken`

En todas las capas se pasa el parámetro `CancellationToken cancellationToken`. Si el cliente HTTP cancela la petición (por ejemplo, el usuario cierra la pestaña del navegador antes de que termine la consulta o el resumen IA):
* El token se cancela.
* La base de datos y las llamadas HTTP externas cortan la operación de inmediato.
* El servidor ahorra CPU, memoria y conexiones de base de datos en peticiones que ya nadie espera.
