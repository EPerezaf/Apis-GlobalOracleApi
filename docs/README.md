# 📚 Documentación GlobalOracleAPI

Esta carpeta contiene toda la documentación del proyecto GlobalOracleAPI.

---

## 📖 Documentos Disponibles

### 🏗️ [ARCHITECTURE.md](./ARCHITECTURE.md)
**Arquitectura y Estrategia del Proyecto**

Documento principal que describe:
- Principios fundamentales de la arquitectura
- Estructura de carpetas recomendada
- Nomenclatura de proyectos, carpetas y namespaces
- Estrategia de módulos y bundles
- Organización de Shared (común vs dominio)
- Dominios compartidos
- Estrategia de endpoints y versionado
- Reglas y convenciones
- Checklist para nuevos módulos

**📌 Leer primero este documento para entender la arquitectura completa.**

---

### 📦 [MODULE_TEMPLATE.md](./MODULE_TEMPLATE.md)
**Plantilla para Crear Nuevos Módulos**

Guía paso a paso para crear nuevos módulos:
- Pasos detallados de creación
- Comandos dotnet para cada capa
- Estructura de carpetas
- Dependencias por proyecto
- Plantilla de Program.cs con Swagger y Scalar
- Plantillas de appsettings.json
- Ejemplo completo de Controller
- Checklist de creación

**📌 Usar este documento cuando necesites crear un nuevo módulo.**

---

### 📝 [CODING_CONVENTIONS.md](./CODING_CONVENTIONS.md)
**Convenciones de Código**

Estándares de código para mantener consistencia:
- Nomenclatura (clases, métodos, variables, DTOs)
- Organización de archivos por capa
- Convenciones C# (async/await, nullable, excepciones)
- Convenciones de Controllers
- Convenciones de Servicios y Repositorios
- Convenciones de DTOs y Entidades
- Seguridad y logging
- Checklist de código

**📌 Consultar este documento al escribir código nuevo.**

---

## 🚀 Inicio Rápido

### Para entender la arquitectura:
1. Lee [ARCHITECTURE.md](./ARCHITECTURE.md)

### Para crear un nuevo módulo:
1. Consulta [MODULE_TEMPLATE.md](./MODULE_TEMPLATE.md)
2. Sigue los pasos detallados
3. Usa el checklist al final

### Para escribir código:
1. Consulta [CODING_CONVENTIONS.md](./CODING_CONVENTIONS.md)
2. Sigue las convenciones establecidas
3. Usa el checklist antes de hacer commit

---

## 📊 Estructura del Proyecto

```
GlobalOracleAPI/
├── docs/                          # 📚 Documentación (esta carpeta)
│   ├── README.md                  # Índice de documentación
│   ├── ARCHITECTURE.md            # Arquitectura y estrategia
│   ├── MODULE_TEMPLATE.md         # Plantilla para módulos
│   └── CODING_CONVENTIONS.md      # Convenciones de código
│
├── src/
│   ├── Companies/                 # APIs específicas por empresa
│   │   └── GM/
│   │       └── CatalogSync/      # Módulo actual
│   │
│   └── Shared/                    # Funcionalidades compartidas
│       ├── Shared.Contracts/
│       ├── Shared.Exceptions/
│       ├── Shared.Security/
│       └── Shared.Infrastructure/
│
└── GlobalOracleAPI.sln
```

---

## 🎯 Principios Clave

1. **50-100 endpoints por módulo** (ideal)
2. **Nomenclatura:** `{Company}.{Module}.{Layer}`
3. **Shared solo si 2+ módulos lo necesitan**
4. **Dominios compartidos solo si 2+ empresas lo necesitan**
5. **Endpoints:** `/api/v1/{company}/{module}/{resource}`

---

## 📞 Soporte

Para preguntas sobre la arquitectura o convenciones, consulta los documentos correspondientes o contacta al equipo de arquitectura.

---

**Última actualización:** 2025-01-16  
**Versión:** 1.0

