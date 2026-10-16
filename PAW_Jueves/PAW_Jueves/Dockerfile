# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor de depuración y cómo Visual Studio usa este Dockerfile para compilar sus imágenes para una depuración más rápida.

# Esta fase se usa cuando se ejecuta desde VS en modo rápido (valor predeterminado para la configuración de depuración)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Carpeta de las llaves de Data Protection, con permisos para el usuario sin privilegios de la imagen.
# docker-compose monta un volumen aqui para que las llaves sobrevivan al recrear el contenedor.
RUN mkdir -p /app/App_Data/keys && chown -R $APP_UID /app/App_Data
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Esta fase se usa para compilar el proyecto de servicio
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PAW_Jueves/PAW_Jueves.csproj", "PAW_Jueves/"]
RUN dotnet restore "./PAW_Jueves/PAW_Jueves.csproj"
COPY . .
WORKDIR "/src/PAW_Jueves"
RUN dotnet build "./PAW_Jueves.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase se usa para publicar el proyecto de servicio que se copiará en la fase final.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PAW_Jueves.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase se usa en producción o cuando se ejecuta desde VS en modo normal (valor predeterminado cuando no se usa la configuración de depuración)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PAW_Jueves.dll"]