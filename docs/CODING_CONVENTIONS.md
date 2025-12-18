# 📝 Convenciones de Código - GlobalOracleAPI

Este documento establece las convenciones de código para mantener consistencia en todo el proyecto.

---

## 🏷️ Nomenclatura

### Clases y Archivos

**Formato:** `PascalCase`

```csharp
✅ CORRECTO:
public class OrderService { }
public class ProductRepository { }
public class CreateOrderDto { }

❌ INCORRECTO:
public class orderService { }
public class product_repository { }
public class createOrderDTO { }
```

### Interfaces

**Formato:** `I` + `PascalCase`

```csharp
✅ CORRECTO:
public interface IOrderService { }
public interface IProductRepository { }

❌ INCORRECTO:
public interface OrderService { }
public interface IorderService { }
```

### Métodos

**Formato:** `PascalCase`

```csharp
✅ CORRECTO:
public async Task<OrderDto> GetOrderByIdAsync(int id) { }
public async Task CreateOrderAsync(CreateOrderDto dto) { }

❌ INCORRECTO:
public async Task<OrderDto> getOrderById(int id) { }
public async Task create_order(CreateOrderDto dto) { }
```

### Variables y Parámetros

**Formato:** `camelCase`

```csharp
✅ CORRECTO:
var orderService = new OrderService();
var orderId = 123;
public void ProcessOrder(int orderId, string customerName) { }

❌ INCORRECTO:
var OrderService = new OrderService();
var order_id = 123;
public void ProcessOrder(int OrderId, string CustomerName) { }
```

### Constantes

**Formato:** `PascalCase` o `UPPER_CASE`

```csharp
✅ CORRECTO:
public const int MaxRetryAttempts = 3;
public const string DEFAULT_CONNECTION_STRING = "...";

❌ INCORRECTO:
public const int maxRetryAttempts = 3;
public const string default_connection_string = "...";
```

### DTOs

**Formato:** `{Action}{Entity}Dto`

```csharp
✅ CORRECTO:
public class CreateOrderDto { }
public class UpdateOrderDto { }
public class OrderDto { }
public class OrderSummaryDto { }

❌ INCORRECTO:
public class OrderCreateDto { }
public class OrderUpdateDto { }
public class OrderDTO { }
```

---

## 📁 Organización de Archivos

### Estructura de Carpetas por Capa

#### API (Controllers)
```
Controllers/
├── OrdersController.cs
├── ProductsController.cs
└── CustomersController.cs
```

#### Application (Services, DTOs)
```
Application/
├── DTOs/
│   ├── OrderDto.cs
│   ├── CreateOrderDto.cs
│   └── UpdateOrderDto.cs
├── Services/
│   ├── IOrderService.cs
│   └── OrderService.cs
└── Exceptions/
    └── OrderExceptions.cs
```

#### Domain (Entities, Interfaces)
```
Domain/
├── Entities/
│   ├── Order.cs
│   └── Product.cs
├── Interfaces/
│   ├── IOrderRepository.cs
│   └── IProductRepository.cs
└── ValueObjects/
    └── Money.cs
```

#### Infrastructure (Repositories)
```
Infrastructure/
├── Repositories/
│   ├── OrderRepository.cs
│   └── ProductRepository.cs
└── Mappings/
    └── OrderMapping.cs
```

---

## 🎯 Convenciones de Código C#

### Usings

**Orden:**
1. System
2. System.Collections.Generic
3. System.Threading.Tasks
4. Microsoft.*
5. Third-party libraries
6. Local namespaces

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog;
using GM.Sales.Application.Services;
using GM.Sales.Domain.Interfaces;
```

### Async/Await

**Siempre usar `async`/`await` para operaciones asíncronas:**

```csharp
✅ CORRECTO:
public async Task<OrderDto> GetOrderAsync(int id)
{
    return await _repository.GetByIdAsync(id);
}

❌ INCORRECTO:
public Task<OrderDto> GetOrder(int id)
{
    return _repository.GetByIdAsync(id);
}
```

**Sufijo `Async` para métodos asíncronos:**

```csharp
✅ CORRECTO:
public async Task<OrderDto> GetOrderByIdAsync(int id) { }
public async Task CreateOrderAsync(CreateOrderDto dto) { }

❌ INCORRECTO:
public async Task<OrderDto> GetOrderById(int id) { }
public async Task CreateOrder(CreateOrderDto dto) { }
```

### Nullable Reference Types

**Usar nullable reference types cuando sea apropiado:**

```csharp
✅ CORRECTO:
public OrderDto? GetOrderById(int id)
{
    return _repository.GetById(id); // Puede retornar null
}

public string GetCustomerName(int id)
{
    return _repository.GetCustomerName(id) ?? "Unknown";
}
```

### Excepciones

**Usar excepciones específicas del dominio:**

```csharp
✅ CORRECTO:
if (order == null)
{
    throw new NotFoundException($"Order with id {id} not found");
}

if (order.Status == OrderStatus.Cancelled)
{
    throw new BusinessValidationException("Cannot update a cancelled order");
}

❌ INCORRECTO:
if (order == null)
{
    throw new Exception("Order not found");
}
```

### Logging

**Usar structured logging:**

```csharp
✅ CORRECTO:
_logger.LogInformation("Order {OrderId} created successfully", orderId);
_logger.LogError(ex, "Error creating order {OrderId}", orderId);
_logger.LogWarning("Order {OrderId} has low stock", orderId);

❌ INCORRECTO:
_logger.LogInformation($"Order {orderId} created successfully");
_logger.LogError("Error creating order: " + ex.Message);
```

---

## 🛣️ Convenciones de Controllers

### Route Attributes

**Formato:** `/api/v{version}/{company}/{module}/{resource}`

```csharp
[ApiController]
[Route("api/v1/gm/sales/orders")]
public class OrdersController : ControllerBase
{
    // ...
}
```

### Métodos HTTP

```csharp
[HttpGet]                    // GET /api/v1/gm/sales/orders
[HttpGet("{id}")]            // GET /api/v1/gm/sales/orders/{id}
[HttpPost]                   // POST /api/v1/gm/sales/orders
[HttpPut("{id}")]           // PUT /api/v1/gm/sales/orders/{id}
[HttpPatch("{id}")]         // PATCH /api/v1/gm/sales/orders/{id}
[HttpDelete("{id}")]        // DELETE /api/v1/gm/sales/orders/{id}
```

### Respuestas

**Usar `ApiResponse<T>` de Shared.Contracts:**

```csharp
✅ CORRECTO:
[HttpGet]
public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetOrders()
{
    var orders = await _orderService.GetAllOrdersAsync();
    return Ok(ApiResponse<List<OrderDto>>.Success(orders));
}

[HttpGet("{id}")]
public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(int id)
{
    var order = await _orderService.GetOrderByIdAsync(id);
    if (order == null)
    {
        return NotFound(ApiResponse<OrderDto>.Error("Order not found"));
    }
    return Ok(ApiResponse<OrderDto>.Success(order));
}

[HttpPost]
public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder([FromBody] CreateOrderDto dto)
{
    try
    {
        var order = await _orderService.CreateOrderAsync(dto);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, 
            ApiResponse<OrderDto>.Success(order));
    }
    catch (BusinessValidationException ex)
    {
        return BadRequest(ApiResponse<OrderDto>.Error(ex.Message));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating order");
        return StatusCode(500, ApiResponse<OrderDto>.Error("Internal server error"));
    }
}
```

### Validación

**Usar Data Annotations o FluentValidation:**

```csharp
✅ CORRECTO:
public class CreateOrderDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    [Required]
    [MinLength(1)]
    public List<OrderItemDto> Items { get; set; } = new();
}
```

---

## 🔄 Convenciones de Servicios

### Dependency Injection

**Usar interfaces para servicios:**

```csharp
✅ CORRECTO:
public interface IOrderService
{
    Task<OrderDto> GetOrderByIdAsync(int id);
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository repository, ILogger<OrderService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // Implementación...
}
```

### Registro en Program.cs

```csharp
✅ CORRECTO:
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
```

---

## 🗄️ Convenciones de Repositorios

### Interfaces

```csharp
✅ CORRECTO:
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<List<Order>> GetAllAsync();
    Task<Order> CreateAsync(Order order);
    Task<Order> UpdateAsync(Order order);
    Task DeleteAsync(int id);
}
```

### Implementación

```csharp
✅ CORRECTO:
public class OrderRepository : IOrderRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(IOracleConnectionFactory connectionFactory, ILogger<OrderRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        // Implementación...
    }
}
```

---

## 📊 Convenciones de DTOs

### Naming

```csharp
✅ CORRECTO:
public class OrderDto { }              // Para lectura
public class CreateOrderDto { }         // Para creación
public class UpdateOrderDto { }         // Para actualización
public class OrderSummaryDto { }        // Para resúmenes
```

### Propiedades

```csharp
✅ CORRECTO:
public class CreateOrderDto
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    [MinLength(1)]
    public List<OrderItemDto> Items { get; set; } = new();

    public string? Notes { get; set; }  // Opcional
}
```

---

## 🏗️ Convenciones de Entidades

### Naming

```csharp
✅ CORRECTO:
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
```

### Propiedades

- **Id:** Siempre `int` o `Guid`
- **Timestamps:** `DateTime` o `DateTimeOffset`
- **Collections:** Inicializar con `= new()`

---

## 🔐 Convenciones de Seguridad

### JWT

**Usar helpers de Shared.Security:**

```csharp
✅ CORRECTO:
var userId = JwtUserHelper.GetUserId(HttpContext.User);
var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
```

### Autorización

```csharp
✅ CORRECTO:
[Authorize]
[HttpGet]
public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetOrders()
{
    // ...
}

[Authorize(Roles = "Admin")]
[HttpDelete("{id}")]
public async Task<ActionResult> DeleteOrder(int id)
{
    // ...
}
```

---

## 📝 Comentarios y Documentación

### XML Comments

**Documentar métodos públicos:**

```csharp
✅ CORRECTO:
/// <summary>
/// Obtiene una orden por su identificador
/// </summary>
/// <param name="id">Identificador de la orden</param>
/// <returns>La orden encontrada o null si no existe</returns>
public async Task<OrderDto?> GetOrderByIdAsync(int id)
{
    // ...
}
```

### Comentarios Inline

**Solo cuando sea necesario explicar "por qué", no "qué":**

```csharp
✅ CORRECTO:
// Usar conexión directa porque el pool está saturado
using var connection = _connectionFactory.CreateDirectConnection();

❌ INCORRECTO:
// Obtener orden por ID
var order = await _repository.GetByIdAsync(id);
```

---

## ✅ Checklist de Código

Antes de hacer commit, verificar:

- [ ] Nomenclatura consistente (PascalCase, camelCase)
- [ ] Métodos async tienen sufijo `Async`
- [ ] Uso de `ApiResponse<T>` en controllers
- [ ] Logging estructurado
- [ ] Manejo de excepciones apropiado
- [ ] Validación de DTOs
- [ ] XML comments en métodos públicos
- [ ] Sin código comentado
- [ ] Sin warnings de compilación
- [ ] Código compila sin errores

---

**Última actualización:** 2025-01-16
**Versión:** 1.0

