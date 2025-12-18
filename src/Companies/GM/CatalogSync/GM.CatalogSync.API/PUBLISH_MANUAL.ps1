# 🚀 Script de Publicación Manual - GM.CatalogSync.API
# Este script realiza el build y publicación de la aplicación

param(
    [string]$PublishPath = "C:\inetpub\wwwroot\GM.CatalogSync.API",
    [string]$Configuration = "Release"
)

Write-Host "🚀 Iniciando publicación de GM.CatalogSync.API..." -ForegroundColor Cyan

# 1. Navegar al directorio del proyecto
$projectPath = "src/Companies/GM/CatalogSync/GM.CatalogSync.API/GM.CatalogSync.API.csproj"
$rootPath = Split-Path -Parent $PSScriptRoot
$rootPath = Split-Path -Parent $rootPath
$rootPath = Split-Path -Parent $rootPath

Set-Location $rootPath
Write-Host "📁 Directorio de trabajo: $rootPath" -ForegroundColor Green

# 2. Limpiar builds anteriores
Write-Host "🧹 Limpiando builds anteriores..." -ForegroundColor Yellow
dotnet clean $projectPath -c $Configuration

# 3. Restaurar paquetes
Write-Host "📦 Restaurando paquetes NuGet..." -ForegroundColor Yellow
dotnet restore $projectPath

# 4. Compilar
Write-Host "🔨 Compilando proyecto..." -ForegroundColor Yellow
dotnet build $projectPath -c $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error en la compilación" -ForegroundColor Red
    exit 1
}

# 5. Crear carpeta de publicación si no existe (con verificación de permisos)
if (-not (Test-Path $PublishPath)) {
    Write-Host "📁 Creando carpeta de publicación: $PublishPath" -ForegroundColor Yellow
    try {
        New-Item -ItemType Directory -Path $PublishPath -Force | Out-Null
        Write-Host "✅ Carpeta creada exitosamente" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Error al crear carpeta: $_" -ForegroundColor Red
        Write-Host "💡 Soluciones:" -ForegroundColor Yellow
        Write-Host "   1. Ejecutar PowerShell como Administrador" -ForegroundColor White
        Write-Host "   2. Crear la carpeta manualmente: New-Item -ItemType Directory -Path '$PublishPath' -Force" -ForegroundColor White
        Write-Host "   3. Dar permisos: icacls '$PublishPath' /grant '$env:USERNAME:(OI)(CI)F'" -ForegroundColor White
        Write-Host "   4. O publicar a una carpeta temporal primero" -ForegroundColor White
        exit 1
    }
}
else {
    # Verificar permisos de escritura
    try {
        $testFile = Join-Path $PublishPath "test_write_permissions.tmp"
        "test" | Out-File -FilePath $testFile -ErrorAction Stop
        Remove-Item $testFile -Force
        Write-Host "✅ Permisos de escritura verificados" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠️  Advertencia: No se tienen permisos de escritura en: $PublishPath" -ForegroundColor Yellow
        Write-Host "💡 Ejecutar como Administrador o dar permisos:" -ForegroundColor Yellow
        Write-Host "   icacls '$PublishPath' /grant '$env:USERNAME:(OI)(CI)F'" -ForegroundColor White
    }
}

# 6. Publicar
Write-Host "📤 Publicando aplicación en: $PublishPath" -ForegroundColor Yellow
dotnet publish $projectPath `
    -c $Configuration `
    -o $PublishPath `
    --self-contained false `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error en la publicación" -ForegroundColor Red
    exit 1
}

# 7. Verificar archivos publicados
Write-Host "✅ Verificando archivos publicados..." -ForegroundColor Yellow
$requiredFiles = @(
    "GM.CatalogSync.API.dll",
    "web.config",
    "appsettings.json"
)

$missingFiles = @()
foreach ($file in $requiredFiles) {
    $filePath = Join-Path $PublishPath $file
    if (-not (Test-Path $filePath)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host "⚠️  Archivos faltantes:" -ForegroundColor Yellow
    foreach ($file in $missingFiles) {
        Write-Host "   - $file" -ForegroundColor Yellow
    }
} else {
    Write-Host "✅ Todos los archivos requeridos están presentes" -ForegroundColor Green
}

# 8. Mostrar resumen
Write-Host "`n📊 Resumen de publicación:" -ForegroundColor Cyan
Write-Host "   📁 Ruta: $PublishPath" -ForegroundColor White
Write-Host "   📦 Configuración: $Configuration" -ForegroundColor White
Write-Host "   📄 Archivos: $((Get-ChildItem $PublishPath -File).Count) archivos" -ForegroundColor White
Write-Host "   💾 Tamaño: $([math]::Round((Get-ChildItem $PublishPath -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB, 2)) MB" -ForegroundColor White

Write-Host "`n✅ Publicación completada exitosamente!" -ForegroundColor Green
Write-Host "`n📝 Próximos pasos:" -ForegroundColor Cyan
Write-Host "   1. Configurar Application Pool en IIS" -ForegroundColor White
Write-Host "   2. Crear Website en IIS apuntando a: $PublishPath" -ForegroundColor White
Write-Host "   3. Configurar permisos de carpeta" -ForegroundColor White
Write-Host "   4. Verificar appsettings.json" -ForegroundColor White
Write-Host "   5. Probar en navegador: https://localhost/swagger" -ForegroundColor White
Write-Host "`n📖 Ver guía completa en: README_IIS.md" -ForegroundColor Cyan

