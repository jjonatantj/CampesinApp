# Dockerfile optimizado para CampesinApp (.NET 8)
# Etapa 1: Build y Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias primero (aprovecha la caché de capas)
COPY ["CampesinApp.csproj", "./"]
RUN dotnet restore "CampesinApp.csproj"

# Copiar el resto del código y compilar en modo Release
COPY . .
RUN dotnet publish "CampesinApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Runtime ligero
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Puerto por defecto de ASP.NET Core 8 en contenedor es 8080 (el mismo que usa Google Cloud Run)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CampesinApp.dll"]

