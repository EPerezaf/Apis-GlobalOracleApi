# 🏗️ Arquitectura GlobalOracleAPI

## 📋 Índice
1. [Principios Fundamentales](#principios-fundamentales)
2. [Estructura de Carpetas](#estructura-de-carpetas)
3. [Nomenclatura](#nomenclatura)
4. [Módulos y Bundles](#módulos-y-bundles)
5. [Shared - Funciones Comunes](#shared---funciones-comunes)
6. [Dominios Compartidos](#dominios-compartidos)
7. [Estrategia de Endpoints](#estrategia-de-endpoints)
8. [Ejemplos de Estructura](#ejemplos-de-estructura)
9. [Reglas y Convenciones](#reglas-y-convenciones)

---

## 🎯 Principios Fundamentales

### 1. Separación por Dominio de Negocio
- **NO** por tecnología
- **SÍ** por funcionalidad de negocio
- Cada módulo = 50-100 endpoints máximo

### 2. Estructura Jerárquica
```
Companies → [Empresa] → [Módulo] → [Capas]
```

### 3. Modular Monolith
- Inicio: Un solo deploy
- Crecimiento: Extracción a microservicios cuando sea necesario
- Aislamiento claro entre módulos

---

## 📁 Estructura de Carpetas

### Estructura Base Recomendada

```
GlobalOracleAPI/
├── src/
│   ├── Companies/                    # APIs específicas por empresa
│   │   ├── GM/                       # General Motors
│   │   │   ├── CatalogSync/          # Módulo: Sincronización de catálogos
│   │   │   │   ├── GM.CatalogSync.API
│   │   │   │   ├── GM.CatalogSync.Application
│   │   │   │   ├── GM.CatalogSync.Domain
│   │   │   │   └── GM.CatalogSync.Infrastructure
│   │   │   ├── Sales/                # Módulo: Ventas
│   │   │   │   ├── GM.Sales.API
│   │   │   │   ├── GM.Sales.Application
│   │   │   │   ├── GM.Sales.Domain
│   │   │   │   └── GM.Sales.Infrastructure
│   │   │   ├── PostSales/            # Módulo: Post-venta
│   │   │   ├── Integrations/         # Módulo: Integraciones
│   │   │   └── Reports/              # Módulo: Reportes
│   │   ├── Jetour/                   # Empresa Jetour
│   │   │   ├── Sales/
│   │   │   ├── PostSales/
│   │   │   └── Integrations/
│   │   ├── Nissan/                   # Empresa Nissan
│   │   │   ├── Reports/
│   │   │   └── Inventory/
│   │   └── [Otras empresas]/
│   │
│   ├── Shared/                       # Funcionalidades compartidas
│   │   ├── Shared.Contracts/        # DTOs, Responses comunes
│   │   ├── Shared.Exceptions/        # Excepciones base
│   │   ├── Shared.Security/          # JWT, Autenticación
│   │   ├── Shared.Infrastructure/    # Conexiones, Factories
│   │   └── Shared.Domain/            # Dominios compartidos (KPI, etc.)
│   │
│   └── Domains/                      # Dominios transversales (opcional)
│       ├── KPI/                      # Si es usado por 2+ empresas
│       │   ├── KPI.API
│       │   ├── KPI.Application
│       │   ├── KPI.Domain
│       │   └── KPI.Infrastructure
│       └── [Otros dominios comunes]/
```

---

## 🏷️ Nomenclatura

### Proyectos (Archivos .csproj)

**Formato:** `{Company}.{Module}.{Layer}`

#### Ejemplos:
```
✅ CORRECTO:
- GM.CatalogSync.API
- GM.CatalogSync.Application
- GM.CatalogSync.Domain
- GM.CatalogSync.Infrastructure

- Jetour.Sales.API
- Jetour.Sales.Application
- Nissan.Reports.API

❌ INCORRECTO:
- GMAPI
- CatalogSyncAPI
- GM_CatalogSync_API
- GM.CatalogSyncAPI
```

### Carpetas Físicas

**Formato:** `src/Companies/{Company}/{Module}/{Project}`

#### Ejemplos:
```
src/Companies/GM/CatalogSync/GM.CatalogSync.API/
src/Companies/GM/CatalogSync/GM.CatalogSync.Application/
src/Companies/Jetour/Sales/Jetour.Sales.API/
```

### Namespaces

**Formato:** `{Company}.{Module}.{Layer}`

#### Ejemplos:
```csharp
namespace GM.CatalogSync.API.Controllers;
namespace GM.CatalogSync.Application.Services;
namespace GM.CatalogSync.Domain.Entities;
namespace GM.CatalogSync.Infrastructure.Repositories;
```

---

## 📦 Módulos y Bundles

### Definición de Módulo

Un **módulo** es una unidad funcional completa que:
- Tiene 50-100 endpoints (ideal)
- Representa un dominio de negocio específico
- Puede evolucionar independientemente
- Tiene sus propias capas (API, Application, Domain, Infrastructure)

### Módulos Recomendados por Empresa

#### Módulos Comunes (pueden existir en múltiples empresas):
- **Sales** - Gestión de ventas
- **PostSales** - Post-venta, servicios
- **Inventory** - Inventario
- **Customers** - Gestión de clientes
- **Reports** - Reportes y analytics
- **Integrations** - Integraciones externas
- **CatalogSync** - Sincronización de catálogos
- **Billing** - Facturación
- **Appointments** - Citas y agendamiento

#### Módulos Específicos (solo para ciertas empresas):
- **GM.CatalogSync** - Específico de GM
- **Nissan.Reports** - Reportes específicos de Nissan

### Regla de Oro para Módulos

> **Si un módulo supera los 100 endpoints, divídelo en submódulos**

Ejemplo:
```
GM.Sales.API (120 endpoints) ❌
↓
GM.Sales.Orders.API (60 endpoints) ✅
GM.Sales.Quotes.API (40 endpoints) ✅
```

---

## 🔄 Shared - Funciones Comunes

### Estructura de Shared

```
Shared/
├── Shared.Contracts/          # DTOs, Responses, Requests comunes
├── Shared.Exceptions/          # Excepciones base
├── Shared.Security/            # JWT, Autenticación, Helpers
├── Shared.Infrastructure/      # Conexiones DB, Factories
└── Shared.Domain/              # Entidades de dominio compartidas
```

### ¿Qué va en Shared?

#### ✅ SÍ va en Shared:
- **Responses base** (`ApiResponse<T>`, `PagedResult<T>`)
- **Excepciones base** (`BusinessException`, `NotFoundException`)
- **Autenticación** (JWT helpers, Claims)
- **Infraestructura común** (ConnectionFactory, Logging)
- **Validaciones comunes** (Attributes, Validators base)
- **Helpers transversales** (DateTimeHelper, CorrelationHelper)

#### ❌ NO va en Shared:
- Lógica de negocio específica
- DTOs específicos de un módulo
- Repositorios específicos
- Servicios de negocio

### Regla de Oro para Shared

> **Shared solo puede crecer si 2+ módulos lo necesitan**

Si solo un módulo lo necesita → va en ese módulo.

---

## 🌐 Dominios Compartidos

### ¿Cuándo crear un Dominio Compartido?

Un dominio compartido (`src/Domains/`) se crea cuando:
1. **2+ empresas** lo necesitan
2. **2+ módulos** lo necesitan
3. Es un **dominio transversal** (KPI, Analytics, Notifications)

### Ejemplo: Dominio KPI

```
Domains/
└── KPI/
    ├── KPI.API
    ├── KPI.Application
    ├── KPI.Domain
    └── KPI.Infrastructure
```

**Uso:**
- `GM.Sales.API` → consume `KPI.API`
- `Jetour.Reports.API` → consume `KPI.API`
- `Nissan.Reports.API` → consume `KPI.API`

### ¿Dominio Compartido vs Shared.Domain?

| Aspecto | Dominio Compartido | Shared.Domain |
|---------|-------------------|---------------|
| **Tiene API propia** | ✅ Sí | ❌ No |
| **Tiene endpoints** | ✅ Sí | ❌ No |
| **Es un servicio** | ✅ Sí | ❌ No |
| **Es una entidad/DTO** | ❌ No | ✅ Sí |

**Ejemplo:**
- `KPI` → Dominio Compartido (tiene API, endpoints)
- `BaseEntity`, `AuditEntity` → Shared.Domain (solo clases base)

---

## 🛣️ Estrategia de Endpoints

### Estructura de URLs

**Formato:** `/api/v{version}/{company}/{module}/{resource}`

#### Ejemplos:
```
GET    /api/v1/gm/catalog-sync/products
POST   /api/v1/gm/catalog-sync/products
GET    /api/v1/gm/catalog-sync/products/{id}
DELETE /api/v1/gm/catalog-sync/products/{id}

GET    /api/v1/jetour/sales/orders
POST   /api/v1/jetour/sales/orders
GET    /api/v1/jetour/sales/orders/{id}

GET    /api/v1/nissan/reports/sales-summary
```

### Convenciones de Endpoints

1. **Plural** para recursos: `/products`, `/orders`
2. **kebab-case** para URLs: `/catalog-sync`, `/post-sales`
3. **Verbos HTTP** claros:
   - `GET` - Consultar
   - `POST` - Crear
   - `PUT` - Actualizar completo
   - `PATCH` - Actualizar parcial
   - `DELETE` - Eliminar

### Versionado

**Estrategia:** Versionado por módulo

```
/api/v1/gm/catalog-sync/products
/api/v2/gm/catalog-sync/products  ← Nueva versión del módulo CatalogSync
```

**NO versionar toda la API:**
```
❌ /api/v2/gm/catalog-sync/products
   /api/v2/jetour/sales/orders
```

---

## 📚 Ejemplos de Estructura

### Ejemplo 1: GM con múltiples módulos

```
src/Companies/GM/
├── CatalogSync/
│   ├── GM.CatalogSync.API          # 15 endpoints
│   ├── GM.CatalogSync.Application
│   ├── GM.CatalogSync.Domain
│   └── GM.CatalogSync.Infrastructure
│
├── Sales/
│   ├── GM.Sales.API                # 80 endpoints
│   ├── GM.Sales.Application
│   ├── GM.Sales.Domain
│   └── GM.Sales.Infrastructure
│
├── PostSales/
│   ├── GM.PostSales.API            # 60 endpoints
│   ├── GM.PostSales.Application
│   ├── GM.PostSales.Domain
│   └── GM.PostSales.Infrastructure
│
└── Integrations/
    ├── GM.Integrations.API         # 45 endpoints
    ├── GM.Integrations.Application
    ├── GM.Integrations.Domain
    └── GM.Integrations.Infrastructure
```

**Total GM:** ~200 endpoints distribuidos en 4 módulos

### Ejemplo 2: Múltiples empresas

```
src/Companies/
├── GM/
│   ├── CatalogSync/
│   ├── Sales/
│   └── PostSales/
│
├── Jetour/
│   ├── Sales/
│   ├── PostSales/
│   └── Integrations/
│
└── Nissan/
    ├── Reports/
    └── Inventory/
```

### Ejemplo 3: Con Dominio Compartido

```
src/
├── Companies/
│   ├── GM/
│   │   └── Sales/
│   ├── Jetour/
│   │   └── Reports/
│   └── Nissan/
│       └── Reports/
│
└── Domains/
    └── KPI/                        # Usado por GM, Jetour, Nissan
        ├── KPI.API
        ├── KPI.Application
        ├── KPI.Domain
        └── KPI.Infrastructure
```

---

## 📏 Reglas y Convenciones

### Regla 1: Tamaño de Módulos
- **Mínimo:** 10 endpoints
- **Ideal:** 50-100 endpoints
- **Máximo:** 100 endpoints (luego dividir)

### Regla 2: Nomenclatura Consistente
- **Proyectos:** `{Company}.{Module}.{Layer}`
- **Carpetas:** `src/Companies/{Company}/{Module}/{Project}`
- **Namespaces:** `{Company}.{Module}.{Layer}`

### Regla 3: Dependencias
- **API** → Application, Domain, Shared.*
- **Application** → Domain, Shared.*
- **Domain** → Solo Shared.Contracts (si es necesario)
- **Infrastructure** → Domain, Shared.*

### Regla 4: Shared
- Solo si 2+ módulos lo necesitan
- No lógica de negocio
- Solo infraestructura y contratos

### Regla 5: Dominios Compartidos
- Solo si 2+ empresas/módulos lo necesitan
- Debe tener API propia
- Es un servicio independiente

### Regla 6: Endpoints
- Plural para recursos
- kebab-case para URLs
- Versionado por módulo

### Regla 7: appsettings.json
- Cada API tiene sus propios `appsettings.json`
- 3 archivos por API: base, Development, Production
- No compartir configuración entre APIs

---

## 🚀 Migración desde Proyecto Monolítico

### Fase 1: Congelar Crecimiento
- No agregar más endpoints al proyecto actual
- Documentar endpoints existentes

### Fase 2: Identificar Dominios
- Agrupar endpoints por funcionalidad
- Identificar dependencias

### Fase 3: Extraer Módulos
- Empezar con módulos menos dependientes
- Mantener mismo contrato REST
- Misma base de datos (inicialmente)

### Fase 4: Evolución
- Separar bases de datos si es necesario
- Extraer a microservicios cuando duela

---

## 📊 Resumen Ejecutivo

### ✅ Sí, separar ya
- 50-100 endpoints por módulo
- Estructura: `Companies/{Company}/{Module}/`
- Nomenclatura consistente

### 🧩 Modular Monolith
- Un solo deploy inicial
- Módulos claramente aislados
- Evolución a microservicios cuando sea necesario

### 🔐 Shared Mínimo
- Solo infraestructura y contratos
- No lógica de negocio
- Solo si 2+ módulos lo necesitan

### 🌐 Dominios Compartidos
- Solo si 2+ empresas/módulos lo necesitan
- Debe tener API propia

### 📛 Nomenclatura Clara
- `{Company}.{Module}.{Layer}`
- Consistente en proyectos, carpetas y namespaces

---

## 📝 Checklist para Nuevos Módulos

- [ ] ¿El módulo tiene 10-100 endpoints?
- [ ] ¿Sigue la nomenclatura `{Company}.{Module}.{Layer}`?
- [ ] ¿Está en la carpeta correcta `src/Companies/{Company}/{Module}/`?
- [ ] ¿Tiene sus 4 capas (API, Application, Domain, Infrastructure)?
- [ ] ¿Usa Shared solo para infraestructura común?
- [ ] ¿Los endpoints siguen el formato `/api/v1/{company}/{module}/{resource}`?
- [ ] ¿Tiene sus propios `appsettings.json`?

---

**Última actualización:** 2025-01-16
**Versión:** 1.0

