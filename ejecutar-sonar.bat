@echo off
chcp 65001 > nul
echo ========================================================
echo   EJECUTOR DE ANÁLISIS SONARQUBE CON COBERTURA DE CÓDIGO
echo   CampesinApp - .NET 8
echo ========================================================
echo.

set SONAR_HOST=http://localhost:9000
set SONAR_PROJECT_KEY=CampesinApp

if "%~1"=="" (
    echo [INFO] No se proporcionó Token de SonarQube como parámetro.
    echo Si tu SonarQube requiere Token, escribe el token a continuación o presiona ENTER para omitir:
    set /p SONAR_TOKEN="Token (opcional): "
) else (
    set SONAR_TOKEN=%~1
)

echo.
echo [1/5] Verificando herramienta dotnet-sonarscanner...
dotnet tool list -g | findstr /i "dotnet-sonarscanner" > nul
if errorlevel 1 (
    echo [AVISO] dotnet-sonarscanner no está instalado globalmente. Instalando...
    dotnet tool install --global dotnet-sonarscanner
)

echo.
echo [2/5] Limpiando resultados anteriores...
if exist "TestResults" rd /s /q "TestResults"

echo.
echo [3/5] Iniciando escáner de SonarQube...
if "%SONAR_TOKEN%"=="" (
    dotnet sonarscanner begin /k:"%SONAR_PROJECT_KEY%" /d:sonar.host.url="%SONAR_HOST%" /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml" /d:sonar.coverage.exclusions="CampesinApp.Tests/**,**/Migrations/**,wwwroot/**,Program.cs"
) else (
    dotnet sonarscanner begin /k:"%SONAR_PROJECT_KEY%" /d:sonar.host.url="%SONAR_HOST%" /d:sonar.token="%SONAR_TOKEN%" /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml" /d:sonar.coverage.exclusions="CampesinApp.Tests/**,**/Migrations/**,wwwroot/**,Program.cs"
)

if errorlevel 1 (
    echo [ERROR] No se pudo iniciar el SonarScanner. Revisa que SonarQube esté encendido en %SONAR_HOST% o tu Token.
    pause
    exit /b %errorlevel%
)

echo.
echo [4/5] Compilando la solución y ejecutando pruebas con OpenCover...
dotnet build
dotnet test CampesinApp.Tests/CampesinApp.Tests.csproj --no-build --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory ./TestResults

echo.
echo [5/5] Finalizando análisis y subiendo cobertura a SonarQube...
if "%SONAR_TOKEN%"=="" (
    dotnet sonarscanner end
) else (
    dotnet sonarscanner end /d:sonar.token="%SONAR_TOKEN%"
)

echo.
echo ========================================================
echo   ¡ANÁLISIS COMPLETADO EXITOSAMENTE!
echo   Abre tu navegador en: %SONAR_HOST%/dashboard?id=%SONAR_PROJECT_KEY%
echo ========================================================
pause

