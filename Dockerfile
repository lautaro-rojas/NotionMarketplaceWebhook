# 1. Usar la imagen oficial de .NET 10 SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 2. Copiar el archivo de proyecto (usando el nombre REAL)
# El asterisco *.csproj busca cualquier archivo de proyecto, así no fallará por el nombre
COPY *.csproj ./
RUN dotnet restore

# 3. Copiar todo el resto del código
COPY . .

# 4. Publicar la aplicación en modo Release
# Esto genera los archivos .dll optimizados en la carpeta /app/publish
RUN dotnet publish -c Release -o /app/publish

# 5. Imagen final para ejecución (Runtime)
# Esta imagen es más ligera porque no tiene las herramientas de compilación
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# 6. Configuración de puertos para Coolify
# Coolify espera tráfico en el puerto 80 por defecto. Modifico a 8080 porque ya configuré ese puerto en Coolify.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# 7. Punto de entrada (El nombre EXACTO de tu DLL)
ENTRYPOINT ["dotnet", "NotionWebhookService.dll"]