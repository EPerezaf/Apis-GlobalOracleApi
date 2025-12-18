# 🚀 Guía Completa de Publicación Manual en IIS - GM.CatalogSync.API

## 📋 Requisitos Previos

Antes de comenzar, verifica que tengas instalado:

1. **IIS** con los siguientes módulos:
   - ASP.NET Core Module v2
   - .NET Core Hosting Bundle (descargar desde [Microsoft](https://dotnet.microsoft.com/download/dotnet/9.0))
2. **.NET 9.0 Runtime** instalado en el servidor
3. **Certificado SSL** configurado en IIS (para HTTPS en producción)
4. **Oracle Client** instalado si la aplicación se conecta a Oracle

### Verificar Instalación

```powershell
# Verificar versión de .NET instalada
dotnet --version

# Verificar IIS instalado
Get-WindowsFeature -Name IIS-WebServerRole

# Verificar módulo ASP.NET Core
Get-WebGlobalModule | Where-Object {$_.Name -like "*AspNetCore*"}
```

---

## 📦 PASO 1: Build Manual de la Aplicación

### Opción A: Desde PowerShell (Recomendado)

1. **Abrir PowerShell como Administrador**

2. **Navegar al directorio del proyecto:**
```powershell
cd D:\Proyectos\Proyectos.Net\GlobalOracleAPI
```

3. **Limpiar builds anteriores (opcional):**
```powershell
dotnet clean src/Companies/GM/CatalogSync/GM.CatalogSync.API/GM.CatalogSync.API.csproj
```

4. **Restaurar paquetes NuGet:**
```powershell
dotnet restore src/Companies/GM/CatalogSync/GM.CatalogSync.API/GM.CatalogSync.API.csproj
```

5. **Compilar en modo Release:**
```powershell
dotnet build src/Companies/GM/CatalogSync/GM.CatalogSync.API/GM.CatalogSync.API.csproj -c Release
```

6. **Publicar la aplicación:**
```powershell
# Crear carpeta de publicación si no existe
$publishPath = "C:\inetpub\wwwroot\GM.CatalogSync.API"
if (-not (Test-Path $publishPath)) {
    New-Item -ItemType Directory -Path $publishPath -Force
}

# Publicar
dotnet publish `
    src/Companies/GM/CatalogSync/GM.CatalogSync.API/GM.CatalogSync.API.csproj `
    -c Release `
    -o $publishPath `
    --self-contained false `
    --no-restore
```

### Opción B: Comando Único (Todo en uno)

```powershell
cd D:\Proyectos\Proyectos.Net\GlobalOracleAPI

$publishPath = "C:\inetpub\wwwroot\GM.CatalogSync.API"

# Limpiar, restaurar, compilar y publicar
dotnet publish `
    src/Companies/GM/CatalogSync/GM.CatalogSync.API/GM.CatalogSync.API.csproj `
    -c Release `
    -o $publishPath `
    --self-contained false

Write-Host "✅ Publicación completada en: $publishPath" -ForegroundColor Green
```

### Verificar Publicación

Después de publicar, verifica que existan estos archivos en la carpeta de destino:

- ✅ `GM.CatalogSync.API.dll`
- ✅ `web.config`
- ✅ `appsettings.json`
- ✅ `appsettings.Production.json` (si existe)
- ✅ Carpeta `wwwroot/` (si existe)

```powershell
# Verificar archivos publicados
Get-ChildItem C:\inetpub\wwwroot\GM.CatalogSync.API | Select-Object Name, Length
```

---

## 🔧 PASO 2: Configuración en IIS

### 2.1 Crear el Application Pool

1. **Abrir IIS Manager:**
   - Presionar `Win + R`, escribir `inetmgr` y presionar Enter
   - O buscar "Internet Information Services (IIS) Manager" en el menú Inicio

2. **Crear Application Pool:**
   - En el panel izquierdo, expandir el servidor
   - Click derecho en **Application Pools** → **Add Application Pool...**

3. **Configurar Application Pool:**
   - **Name**: `GM.CatalogSync.API`
   - **.NET CLR Version**: **No Managed Code** ⚠️ (IMPORTANTE para .NET Core/5+)
   - **Managed Pipeline Mode**: **Integrated**
   - Click en **OK**

4. **Configurar Advanced Settings:**
   - Click derecho en `GM.CatalogSync.API` → **Advanced Settings...**
   - Configurar:
     - **Start Mode**: `AlwaysRunning` (opcional, para mejor performance)
     - **Idle Timeout**: `0` (deshabilitar timeout, opcional)
     - **Identity**: `ApplicationPoolIdentity` (por defecto, seguro)
   - Click en **OK**

### 2.2 Crear el Website

1. **Crear Website:**
   - Click derecho en **Sites** → **Add Website...**

2. **Configurar Website:**
   - **Site name**: `GM.CatalogSync.API`
   - **Application pool**: Seleccionar `GM.CatalogSync.API` (el que creamos)
   - **Physical path**: `C:\inetpub\wwwroot\GM.CatalogSync.API`
     - ⚠️ Click en **...** para navegar y seleccionar la carpeta
   - **Binding**:
     - **Type**: `https` (o `http` para desarrollo)
     - **IP address**: `All Unassigned` (o IP específica del servidor)
     - **Port**: `443` (HTTPS) o `80` (HTTP) o puerto personalizado (ej: `5001`)
     - **Host name**: (opcional) `api-catalogsync.gm.local` o dejar vacío
     - **SSL certificate**: Seleccionar certificado válido (si usas HTTPS)
   - Click en **OK**

### 2.3 Configurar Permisos de Carpeta

1. **Abrir propiedades de la carpeta:**
   - Navegar a `C:\inetpub\wwwroot\GM.CatalogSync.API` en el Explorador de Windows
   - Click derecho → **Properties** → Pestaña **Security**

2. **Agregar permisos:**
   - Click en **Edit...**
   - Click en **Add...**
   - Escribir `IIS_IUSRS` → Click **Check Names** → **OK**
   - Seleccionar `IIS_IUSRS` → Marcar:
     - ✅ **Read & execute**
     - ✅ **List folder contents**
     - ✅ **Read**
   - Click en **OK**

3. **Agregar permisos para Application Pool Identity:**
   - Click en **Add...**
   - Escribir `IIS AppPool\GM.CatalogSync.API` → Click **Check Names** → **OK**
   - Seleccionar `IIS AppPool\GM.CatalogSync.API` → Marcar:
     - ✅ **Read & execute**
     - ✅ **List folder contents**
     - ✅ **Read**
   - Click en **OK** → **OK**

4. **Permisos para carpeta Logs (si existe):**
   - Si la carpeta `Logs/` existe, dar permisos de **Write** a `IIS AppPool\GM.CatalogSync.API`

### 2.4 Configurar Variables de Entorno (Opcional)

Si necesitas configurar variables de entorno específicas:

1. **Abrir Configuration Editor:**
   - Click derecho en el sitio `GM.CatalogSync.API` → **Configuration Editor...**

2. **Configurar variables:**
   - En el dropdown superior, seleccionar: `system.webServer/aspNetCore`
   - Expandir `environmentVariables`
   - Click en **...** (Collection Editor)
   - Agregar:
     - **Name**: `ASPNETCORE_ENVIRONMENT`
     - **Value**: `Production`
   - Click en **OK** → **Apply**

### 2.5 Verificar web.config

Asegúrate de que existe `web.config` en la carpeta de publicación. Si no existe, créalo:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" 
                  arguments=".\GM.CatalogSync.API.dll" 
                  stdoutLogEnabled="false" 
                  stdoutLogFile=".\logs\stdout" 
                  hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

---

## ⚙️ PASO 3: Configurar appsettings.json

1. **Editar appsettings.json:**
   - Abrir `C:\inetpub\wwwroot\GM.CatalogSync.API\appsettings.json`

2. **Verificar configuración:**
   - ✅ **ConnectionStrings**: Connection string de Oracle correcta
   - ✅ **Jwt**: Key, Issuer, Audience configurados
   - ✅ **Serilog**: Rutas de logs correctas

3. **Crear appsettings.Production.json (opcional):**
   - Copiar `appsettings.json` a `appsettings.Production.json`
   - Ajustar valores para producción

---

## 🧪 PASO 4: Verificar que Funciona

### 4.1 Iniciar el Application Pool

1. En IIS Manager, click derecho en `GM.CatalogSync.API` (Application Pool)
2. Click en **Start** (si está detenido)

### 4.2 Iniciar el Website

1. En IIS Manager, click derecho en `GM.CatalogSync.API` (Website)
2. Click en **Start** (si está detenido)

### 4.3 Probar en el Navegador

1. **Abrir navegador:**
   - `https://localhost/swagger` (si usas puerto 443)
   - `https://localhost:5001/swagger` (si usas puerto 5001)
   - `http://localhost/swagger` (si usas HTTP)

2. **Verificar Swagger:**
   - Deberías ver la documentación de la API
   - Probar un endpoint GET con token JWT válido

3. **Verificar Scalar (si está configurado):**
   - `https://localhost/scalar` o `https://localhost:5001/scalar`

### 4.4 Verificar Logs

```powershell
# Ver últimos logs
Get-Content C:\inetpub\wwwroot\GM.CatalogSync.API\Logs\log-*.txt -Tail 50
```

---

## 🔐 PASO 5: Configuración de Certificado SSL (Producción)

### 5.1 Obtener Certificado

- **Let's Encrypt** (gratis, recomendado)
- **Certificado comprado** (Comodo, DigiCert, etc.)
- **Certificado autofirmado** (solo para desarrollo/testing)

### 5.2 Importar Certificado

1. **Abrir Certificate Manager:**
   - `Win + R` → `certlm.msc` → Enter

2. **Importar certificado:**
   - Expandir **Personal** → **Certificates**
   - Click derecho → **All Tasks** → **Import...**
   - Seleccionar archivo `.pfx` o `.cer`
   - Ingresar password si es necesario
   - Click en **OK**

### 5.3 Asignar Certificado en IIS

1. En IIS Manager, click en el sitio `GM.CatalogSync.API`
2. Click en **Bindings...** en el panel derecho
3. Seleccionar binding HTTPS → **Edit...**
4. Seleccionar certificado en **SSL certificate**
5. Click en **OK** → **Close**

---

## 🐛 Troubleshooting

### Error: "500.30 - In-Process Start Failure"

**Causas posibles:**
- .NET 9.0 Runtime no instalado
- Permisos incorrectos en la carpeta
- `web.config` mal configurado

**Solución:**
```powershell
# Verificar .NET instalado
dotnet --list-runtimes

# Verificar permisos
icacls C:\inetpub\wwwroot\GM.CatalogSync.API

# Revisar Event Viewer
Get-EventLog -LogName Application -Source "IIS*" -Newest 10
```

### Error: "502.5 - Process Failure"

**Causas posibles:**
- Application Pool mal configurado
- `web.config` no existe o está mal formado
- Variables de entorno incorrectas

**Solución:**
1. Verificar que Application Pool use **No Managed Code**
2. Verificar que `web.config` existe y está bien formado
3. Revisar logs en `C:\inetpub\wwwroot\GM.CatalogSync.API\Logs\`

### Error: "500.0 - ANCM In-Process Handler Load Failure"

**Causas posibles:**
- Módulo ASP.NET Core no instalado
- Versión incorrecta del módulo

**Solución:**
1. Descargar e instalar [.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Reiniciar IIS: `iisreset`

### Puerto no responde

**Causas posibles:**
- Firewall bloqueando el puerto
- Otro servicio usando el puerto
- Binding incorrecto en IIS

**Solución:**
```powershell
# Verificar qué proceso usa el puerto
netstat -ano | findstr :5001

# Abrir puerto en firewall
New-NetFirewallRule -DisplayName "GM.CatalogSync.API" -Direction Inbound -Protocol TCP -LocalPort 5001 -Action Allow
```

### Error de conexión a Oracle

**Causas posibles:**
- Connection string incorrecta
- Oracle Client no instalado
- Permisos de base de datos

**Solución:**
1. Verificar `appsettings.json` → `ConnectionStrings:Oracle`
2. Verificar que Oracle Client esté instalado
3. Probar conexión desde el servidor usando `sqlplus` o similar

---

## 📝 Notas Importantes

- ⚠️ **En producción, IIS maneja el puerto automáticamente** según el binding del sitio
- ⚠️ **No configurar `UseUrls()` en producción** - ya está condicionado en `Program.cs`
- ⚠️ **HTTPS es obligatorio en producción** - configurar certificado válido
- ⚠️ **Logs se guardan en `Logs/`** dentro del directorio de publicación
- ⚠️ **Application Pool Identity** debe tener permisos de lectura en la carpeta

---

## 📚 Recursos Adicionales

- [ASP.NET Core Hosting en IIS](https://learn.microsoft.com/aspnet/core/host-and-deploy/iis/)
- [Configurar HTTPS en IIS](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl)
- [ASP.NET Core Module Configuration Reference](https://learn.microsoft.com/aspnet/core/host-and-deploy/aspnet-core-module)

## ⚙️ Configuración de appsettings.json

Asegúrate de actualizar `appsettings.json` o `appsettings.Production.json` con:

1. **Connection String de Oracle** real
2. **JWT Key** (ya está configurado)
3. **Rutas de logs** (ajustar si es necesario)

## 🔐 Configuración de Certificado SSL

Si usas HTTPS en el puerto 5001:

1. **Obtener certificado** (Let's Encrypt, comprado, o autofirmado para desarrollo)
2. **Importar certificado** en el servidor
3. **Asignar certificado** en el binding de IIS

## 🧪 Verificar que Funciona

1. Abrir navegador: `https://localhost:5001/swagger`
2. Deberías ver la documentación Swagger
3. Probar un endpoint con token JWT válido

## 📝 Notas Importantes

- **Puerto 5001**: Configurado fijo en `Program.cs` y `launchSettings.json`
- **web.config**: Incluido en el proyecto y se copia al publicar
- **Logs**: Se guardan en la carpeta `Logs/` dentro del directorio de publicación
- **HTTPS**: Requerido para producción, configurar certificado válido

## 🐛 Troubleshooting

### Error: "500.30 - In-Process Start Failure"

- Verificar que .NET 9.0 Runtime esté instalado
- Verificar permisos en la carpeta de publicación
- Revisar logs en `Logs/` o Event Viewer

### Error: "502.5 - Process Failure"

- Verificar que el Application Pool esté configurado correctamente
- Verificar `web.config` esté presente
- Revisar variables de entorno

### Puerto 5001 no responde

- Verificar que el binding en IIS esté configurado correctamente
- Verificar firewall (permitir puerto 5001)
- Verificar que no haya otro servicio usando el puerto 5001

## 📚 Recursos Adicionales

- [ASP.NET Core Hosting en IIS](https://learn.microsoft.com/aspnet/core/host-and-deploy/iis/)
- [Configurar HTTPS en IIS](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl)

