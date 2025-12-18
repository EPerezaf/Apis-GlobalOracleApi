# ============================================
# Script de Publicación y Despliegue
# GM.CatalogSync.API - IIS Deployment (Local y Remoto)
# 
# Uso:
#   .\publish-and-deploy-gm-catalogsync.ps1 -ServerName "IP_SERVIDOR" -ServerUser "usuario"
# ============================================

param(
    [string]$Environment = "Release",
    [string]$PublishPath = "C:\inetpub\wwwroot\GM.CatalogSync.API",
    [string]$AppPoolName = "GM.CatalogSync.API",
    [string]$ProjectPath = "src\Companies\GM\CatalogSync\GM.CatalogSync.API",
    [string]$ServerName = "",              # Nombre del servidor remoto (IP o nombre de dominio)
    [string]$ServerUser = "",             # Usuario para conexión remota (ej: dominio\usuario)
    [string]$ServerPassword = "",         # Password para conexión remota (opcional, se pedirá si no se proporciona)
    [switch]$SkipRestart = $false,
    [switch]$Backup = $true,
    [switch]$UseRobocopy = $true          # Usar robocopy para copiar archivos (más rápido y confiable)
)

$ErrorActionPreference = "Stop"
$script:StartTime = Get-Date

# Colores para output
function Write-Step {
    param([string]$Message)
    Write-Host "`n[$($script:StartTime.ToString('HH:mm:ss'))] " -NoNewline -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Green
}

function Write-Error-Step {
    param([string]$Message)
    Write-Host "`n[$($script:StartTime.ToString('HH:mm:ss'))] " -NoNewline -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "  → $Message" -ForegroundColor Yellow
}

# Verificar que estamos en la raíz del proyecto
$rootPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFullPath = Join-Path $rootPath $ProjectPath
$csprojPath = Join-Path $projectFullPath "GM.CatalogSync.API.csproj"

if (-not (Test-Path $csprojPath)) {
    Write-Error-Step "Error: No se encontró el proyecto en $csprojPath"
    Write-Info "Asegúrate de ejecutar el script desde la raíz del proyecto GlobalOracleAPI"
    exit 1
}

# Determinar si es servidor remoto
$isRemote = $false
$remotePublishPath = $PublishPath
$localPublishPath = Join-Path $env:TEMP "GM.CatalogSync.API.Publish"
$serverNameOnly = ""
$serverPort = ""

if (-not [string]::IsNullOrWhiteSpace($ServerName)) {
    $isRemote = $true
    Write-Step "🌐 Modo: Servidor Remoto"
    
    # Separar nombre del servidor y puerto si existe
    if ($ServerName -match '^(.+):(\d+)$') {
        $serverNameOnly = $matches[1]
        $serverPort = $matches[2]
        Write-Info "Servidor: $serverNameOnly (Puerto: $serverPort)"
        Write-Info "⚠️  Nota: El puerto se usará solo para PowerShell Remoting, no para rutas UNC"
    }
    else {
        $serverNameOnly = $ServerName
        Write-Info "Servidor: $ServerName"
    }
    
    # Construir ruta UNC para servidor remoto (sin puerto, las rutas UNC no usan puerto)
    if (-not $PublishPath.StartsWith("\\")) {
        # Convertir C:\ruta a \\servidor\C$\ruta
        $driveLetter = $PublishPath.Substring(0, 1)
        $pathWithoutDrive = $PublishPath.Substring(3)
        $remotePublishPath = "\\$serverNameOnly\$driveLetter`$$pathWithoutDrive"
    }
    else {
        $remotePublishPath = $PublishPath
    }
    
    Write-Info "Ruta local temporal: $localPublishPath"
    Write-Info "Ruta remota destino: $remotePublishPath"
    
    # Solicitar credenciales si no se proporcionaron
    if ([string]::IsNullOrWhiteSpace($ServerUser)) {
        $ServerUser = Read-Host "Ingresa el usuario para conectarte al servidor (ej: dominio\usuario o .\usuario)"
    }
    
    if ([string]::IsNullOrWhiteSpace($ServerPassword)) {
        $securePassword = Read-Host "Ingresa la contraseña" -AsSecureString
        $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
        $ServerPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    }
    
    # Crear objeto de credenciales
    $securePassword = ConvertTo-SecureString $ServerPassword -AsPlainText -Force
    $credential = New-Object System.Management.Automation.PSCredential($ServerUser, $securePassword)
}
else {
    Write-Step "💻 Modo: Servidor Local"
    $localPublishPath = $PublishPath
}

Write-Step "🚀 Iniciando proceso de publicación y despliegue"
Write-Info "Proyecto: $csprojPath"
Write-Info "Destino: $remotePublishPath"
Write-Info "Environment: $Environment"
Write-Info "Application Pool: $AppPoolName"

# ============================================
# PASO 1: Publicar la aplicación localmente
# ============================================
Write-Step "🔨 Publicando aplicación (.NET)"
try {
    Push-Location $rootPath
    
    # Limpiar carpeta temporal si existe
    if (Test-Path $localPublishPath) {
        Remove-Item -Path $localPublishPath -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    $publishCommand = "dotnet publish `"$csprojPath`" -c $Environment -o `"$localPublishPath`" --no-self-contained"
    
    Write-Info "Ejecutando: dotnet publish..."
    $publishOutput = Invoke-Expression $publishCommand 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error en dotnet publish. Exit code: $LASTEXITCODE"
    }
    
    Write-Info "Publicación completada exitosamente"
}
catch {
    Write-Error-Step "Error al publicar la aplicación: $_"
    Write-Info "Output: $publishOutput"
    Pop-Location
    exit 1
}
finally {
    Pop-Location
}

# ============================================
# PASO 2: Verificar archivos publicados
# ============================================
Write-Step "✅ Verificando archivos publicados"
$dllPath = Join-Path $localPublishPath "GM.CatalogSync.API.dll"
$webConfigPath = Join-Path $localPublishPath "web.config"
$appsettingsPath = Join-Path $localPublishPath "appsettings.json"

if (-not (Test-Path $dllPath)) {
    Write-Error-Step "Error: No se encontró GM.CatalogSync.API.dll en $localPublishPath"
    exit 1
}

if (-not (Test-Path $webConfigPath)) {
    Write-Info "⚠️  web.config no encontrado. Creando..."
    $webConfigContent = @"
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
"@
    Set-Content -Path $webConfigPath -Value $webConfigContent -Encoding UTF8
    Write-Info "web.config creado"
}

Write-Info "✓ GM.CatalogSync.API.dll encontrado"
Write-Info "✓ web.config verificado"
Write-Info "✓ appsettings.json verificado"

# ============================================
# PASO 3: Backup en servidor remoto (opcional)
# ============================================
if ($Backup) {
    Write-Step "📦 Creando backup del despliegue anterior"
    try {
        if ($isRemote) {
            # Backup remoto usando Invoke-Command
            $backupScript = @"
if (Test-Path '$PublishPath') {
    `$backupPath = '$PublishPath.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')'
    Copy-Item -Path '$PublishPath' -Destination `$backupPath -Recurse -Force
    Write-Output "Backup creado: `$backupPath"
}
"@
            $backupResult = Invoke-Command -ComputerName $ServerName -Credential $credential -ScriptBlock {
                param($script, $path)
                Invoke-Expression $script
            } -ArgumentList $backupScript, $PublishPath -ErrorAction SilentlyContinue
            
            if ($backupResult) {
                Write-Info "Backup remoto creado exitosamente"
            }
        }
        else {
            # Backup local
            if (Test-Path $PublishPath) {
                $backupPath = "$PublishPath.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
                Copy-Item -Path $PublishPath -Destination $backupPath -Recurse -Force
                Write-Info "Backup creado en: $backupPath"
            }
        }
    }
    catch {
        Write-Info "⚠️  No se pudo crear backup: $_"
        Write-Info "Continuando sin backup..."
    }
}

# ============================================
# PASO 4: Copiar archivos al servidor
# ============================================
Write-Step "📤 Copiando archivos al servidor"
try {
    if ($isRemote) {
        if ($UseRobocopy) {
            # Usar robocopy (más rápido y confiable para remoto)
            Write-Info "Usando robocopy para copiar archivos..."
            
            # Construir comando robocopy con credenciales
            $robocopyArgs = @(
                "`"$localPublishPath`"",
                "`"$remotePublishPath`"",
                "/E",           # Incluir subdirectorios vacíos
                "/Z",           # Modo reinicio
                "/R:3",         # Reintentos: 3
                "/W:5",         # Espera entre reintentos: 5 seg
                "/MT:8",        # Multithread: 8 hilos
                "/NFL",         # No mostrar lista de archivos
                "/NDL",         # No mostrar lista de directorios
                "/NP",          # No mostrar progreso
                "/NC",          # No mostrar clases
                "/NS",          # No mostrar resumen
                "/NJH",         # No mostrar header de trabajo
                "/NJS"          # No mostrar resumen de trabajo
            )
            
            # Ejecutar robocopy con credenciales
            # Extraer la ruta base de la UNC (sin el puerto)
            $netUsePath = $remotePublishPath.Substring(0, $remotePublishPath.LastIndexOf('\'))
            
            Write-Info "Conectando a: $netUsePath"
            Write-Info "Usuario: $ServerUser"
            
            # Intentar desconectar primero si existe conexión previa
            net use $netUsePath /delete /yes 2>&1 | Out-Null
            
            # Conectar con credenciales
            $netUseResult = net use $netUsePath /user:$ServerUser $ServerPassword 2>&1
            
            if ($LASTEXITCODE -ne 0) {
                Write-Error-Step "Error al conectar a la ruta de red: $netUseResult"
                Write-Info "Verifica:"
                Write-Info "  1. Que el servidor sea accesible: $serverNameOnly"
                Write-Info "  2. Que las credenciales sean correctas"
                Write-Info "  3. Que el puerto 445 (SMB) esté abierto en el firewall"
                throw "No se pudo conectar a la ruta de red"
            }
            
            Write-Info "✓ Conexión a ruta de red establecida"
            
            Write-Info "Ejecutando robocopy..."
            $robocopyOutput = & robocopy $robocopyArgs 2>&1
            $robocopyExitCode = $LASTEXITCODE
            
            # Desconectar después de copiar
            net use $netUsePath /delete /yes 2>&1 | Out-Null
            
            # Robocopy retorna códigos especiales (0-7 son exitosos)
            # 0 = Sin errores, no se copió nada
            # 1 = Archivos copiados exitosamente
            # 2 = Archivos adicionales en destino
            # 3-7 = Combinaciones de lo anterior
            if ($robocopyExitCode -le 7) {
                Write-Info "✓ Archivos copiados exitosamente (Exit code: $robocopyExitCode)"
            }
            else {
                Write-Error-Step "Error en robocopy. Exit code: $robocopyExitCode"
                Write-Info "Códigos de error comunes:"
                Write-Info "  8 = Error de memoria"
                Write-Info "  16 = Error grave (acceso denegado, ruta no encontrada, etc.)"
                throw "Error en robocopy. Exit code: $robocopyExitCode"
            }
        }
        else {
            # Usar Copy-Item con credenciales (más lento pero funciona)
            Write-Info "Usando Copy-Item para copiar archivos..."
            
            # Mapear unidad de red
            $driveLetter = "Z:"
            $netUsePath = $remotePublishPath.Substring(0, $remotePublishPath.LastIndexOf('\'))
            net use $driveLetter $netUsePath /user:$ServerUser $ServerPassword | Out-Null
            
            try {
                $mappedPath = $remotePublishPath.Replace($netUsePath, $driveLetter)
                Copy-Item -Path "$localPublishPath\*" -Destination $mappedPath -Recurse -Force
                Write-Info "✓ Archivos copiados exitosamente"
            }
            finally {
                # Desmapear unidad
                net use $driveLetter /delete /yes | Out-Null
            }
        }
    }
    else {
        # Copia local
        if (Test-Path $PublishPath) {
            Remove-Item -Path $PublishPath -Recurse -Force -ErrorAction SilentlyContinue
        }
        Copy-Item -Path "$localPublishPath\*" -Destination $PublishPath -Recurse -Force
        Write-Info "✓ Archivos copiados exitosamente"
    }
}
catch {
    Write-Error-Step "Error al copiar archivos: $_"
    exit 1
}

# ============================================
# PASO 5: Verificar IIS y Application Pool (remoto o local)
# ============================================
Write-Step "🔍 Verificando IIS y Application Pool"

try {
    if ($isRemote) {
        # Verificar remotamente
        $checkScript = @"
Import-Module WebAdministration -ErrorAction SilentlyContinue
`$appPool = Get-WebAppPoolState -Name '$AppPoolName' -ErrorAction SilentlyContinue
if (`$appPool) {
    Write-Output "EXISTS|`$(`$appPool.Value)"
}
else {
    Write-Output "NOTFOUND"
}
"@
            # Usar solo el nombre del servidor sin puerto para Invoke-Command
            $invokeServerName = if (-not [string]::IsNullOrWhiteSpace($serverPort)) {
                # Si hay puerto, usar ConnectionUri (WinRM puede usar puerto personalizado)
                # Nota: WinRM por defecto usa 5985 (HTTP) o 5986 (HTTPS)
                # Si el puerto es diferente, puede requerir configuración adicional
                $serverNameOnly
            } else {
                $serverNameOnly
            }
            
            $checkResult = Invoke-Command -ComputerName $invokeServerName -Credential $credential -ScriptBlock {
                param($script)
                Invoke-Expression $script
            } -ArgumentList $checkScript -ErrorAction SilentlyContinue
        
        if ($checkResult -like "NOTFOUND*") {
            Write-Error-Step "Error: Application Pool '$AppPoolName' no existe en el servidor remoto"
            Write-Info "Crea el Application Pool primero en IIS Manager del servidor remoto"
            exit 1
        }
        
        $appPoolState = ($checkResult -split '\|')[1]
        Write-Info "✓ Application Pool '$AppPoolName' encontrado (Estado: $appPoolState)"
    }
    else {
        # Verificar localmente
        Import-Module WebAdministration -ErrorAction Stop
        Write-Info "Módulo WebAdministration cargado"
        
        $appPool = Get-WebAppPoolState -Name $AppPoolName -ErrorAction SilentlyContinue
        if (-not $appPool) {
            Write-Error-Step "Error: Application Pool '$AppPoolName' no existe"
            Write-Info "Crea el Application Pool primero en IIS Manager"
            exit 1
        }
        
        Write-Info "✓ Application Pool '$AppPoolName' encontrado (Estado: $($appPool.Value))"
    }
}
catch {
    Write-Error-Step "Error al verificar IIS: $_"
    Write-Info "Asegúrate de que IIS Management Tools esté instalado"
    exit 1
}

# ============================================
# PASO 6: Reiniciar Application Pool (remoto o local)
# ============================================
if (-not $SkipRestart) {
    Write-Step "🔄 Reiniciando Application Pool"
    try {
        if ($isRemote) {
            # Reiniciar remotamente
            $restartScript = @"
Import-Module WebAdministration
Stop-WebAppPool -Name '$AppPoolName'
Start-Sleep -Seconds 2
Start-WebAppPool -Name '$AppPoolName'
Start-Sleep -Seconds 3
`$state = (Get-WebAppPoolState -Name '$AppPoolName').Value
Write-Output `$state
"@
            # Usar solo el nombre del servidor sin puerto para Invoke-Command
            $invokeServerName = if (-not [string]::IsNullOrWhiteSpace($serverPort)) {
                $serverNameOnly
            } else {
                $serverNameOnly
            }
            
            $restartResult = Invoke-Command -ComputerName $invokeServerName -Credential $credential -ScriptBlock {
                param($script)
                Invoke-Expression $script
            } -ArgumentList $restartScript -ErrorAction SilentlyContinue
            
            if ($restartResult -eq "Started") {
                Write-Info "✓ Application Pool reiniciado exitosamente"
            }
            else {
                Write-Error-Step "Error: Application Pool no está en estado 'Started' (Estado: $restartResult)"
            }
        }
        else {
            # Reiniciar localmente
            Stop-WebAppPool -Name $AppPoolName -ErrorAction Stop
            Start-Sleep -Seconds 2
            Start-WebAppPool -Name $AppPoolName -ErrorAction Stop
            Start-Sleep -Seconds 3
            
            $appPoolState = (Get-WebAppPoolState -Name $AppPoolName).Value
            if ($appPoolState -eq "Started") {
                Write-Info "✓ Application Pool reiniciado exitosamente"
            }
            else {
                Write-Error-Step "Error: Application Pool no está en estado 'Started' (Estado: $appPoolState)"
            }
        }
    }
    catch {
        Write-Error-Step "Error al reiniciar Application Pool: $_"
        Write-Info "Intenta reiniciarlo manualmente desde IIS Manager"
    }
}
else {
    Write-Info "⏭️  Reinicio de Application Pool omitido (parámetro -SkipRestart)"
}

# ============================================
# PASO 7: Limpiar carpeta temporal
# ============================================
if ($isRemote -and (Test-Path $localPublishPath)) {
    Write-Step "🧹 Limpiando carpeta temporal"
    try {
        Remove-Item -Path $localPublishPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Info "✓ Carpeta temporal eliminada"
    }
    catch {
        Write-Info "⚠️  No se pudo eliminar carpeta temporal: $_"
    }
}

# ============================================
# PASO 8: Resumen final
# ============================================
$endTime = Get-Date
$duration = $endTime - $script:StartTime

Write-Step "✅ Despliegue completado exitosamente"
Write-Info "Tiempo total: $($duration.TotalSeconds.ToString('F2')) segundos"
Write-Info "Archivos desplegados en: $remotePublishPath"
Write-Info ""
Write-Host "📋 Próximos pasos:" -ForegroundColor Cyan
Write-Host "  1. Verifica que el sitio esté funcionando: https://globaldms.mx/swagger" -ForegroundColor White
Write-Host "  2. Prueba un endpoint con tu token JWT" -ForegroundColor White
if ($isRemote) {
    Write-Host "  3. Revisa los logs en el servidor: $remotePublishPath\Logs\" -ForegroundColor White
}
else {
    Write-Host "  3. Revisa los logs en: $PublishPath\Logs\" -ForegroundColor White
}
Write-Host ""

# ============================================
# FIN DEL SCRIPT
# ============================================
