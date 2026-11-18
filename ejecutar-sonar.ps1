param (
    [string]$Token = "",
    [string]$HostUrl = "http://localhost:9000",
    [string]$ProjectKey = "CampesinApp"
)

Write-Host "========================================================" -ForegroundColor Green
Write-Host "  EJECUTOR DE ANÁLISIS SONARQUBE CON COBERTURA DE CÓDIGO" -ForegroundColor Green
Write-Host "  CampesinApp - .NET 8" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host ""

# 1. Verificar herramienta dotnet-sonarscanner
Write-Host "[1/5] Verificando herramienta dotnet-sonarscanner..." -ForegroundColor Cyan
$herramientaInstalada = dotnet tool list -g | Select-String "dotnet-sonarscanner"
if (-not $herramientaInstalada) {
    Write-Host "[AVISO] Instalando dotnet-sonarscanner globalmente..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-sonarscanner
}

# 2. Limpiar resultados anteriores
Write-Host "[2/5] Limpiando carpeta de resultados previa..." -ForegroundColor Cyan
if (Test-Path "TestResults") {
    Remove-Item -Recurse -Force "TestResults"
}

# 3. Preparar parámetros de inicio
Write-Host "[3/5] Iniciando escáner de SonarQube..." -ForegroundColor Cyan
$argsBegin = @(
    "sonarscanner", "begin",
    "/k:$ProjectKey",
    "/d:sonar.host.url=$HostUrl",
    "/d:sonar.cs.opencover.reportsPaths=**/TestResults/**/coverage.opencover.xml",
    "/d:sonar.coverage.exclusions=CampesinApp.Tests/**,**/Migrations/**,wwwroot/**,Program.cs"
)

if (![string]::IsNullOrWhiteSpace($Token)) {
    $argsBegin += "/d:sonar.token=$Token"
}

& dotnet @argsBegin
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] No se pudo conectar con SonarQube en $HostUrl o el Token es inválido." -ForegroundColor Red
    return
}

# 4. Compilar y ejecutar pruebas con recolección de cobertura OpenCover
Write-Host "[4/5] Compilando y ejecutando pruebas unitarias con Coverlet..." -ForegroundColor Cyan
dotnet build
dotnet test CampesinApp.Tests/CampesinApp.Tests.csproj --no-build --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory ./TestResults

# 5. Finalizar análisis
Write-Host "[5/5] Finalizando análisis y enviando métricas a SonarQube..." -ForegroundColor Cyan
$argsEnd = @("sonarscanner", "end")
if (![string]::IsNullOrWhiteSpace($Token)) {
    $argsEnd += "/d:sonar.token=$Token"
}

& dotnet @argsEnd

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "  ¡ANÁLISIS COMPLETADO EXITOSAMENTE!" -ForegroundColor Green
Write-Host "  Consulta el resultado en: $HostUrl/dashboard?id=$ProjectKey" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green

