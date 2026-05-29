FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar proyecto y restaurar
COPY ["Bar_QR/Bar_QR.csproj", "Bar_QR/"]
RUN dotnet restore "Bar_QR/Bar_QR.csproj"

# Copiar todo y publicar
COPY . .
WORKDIR "/src/Bar_QR"
RUN dotnet publish "Bar_QR.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagen final de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Crear directorio de datos para SQLite
RUN mkdir -p /data && chmod 777 /data

# Variables para Railway
ENV ASPNETCORE_URLS=http://*:${PORT}
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE ${PORT}
ENTRYPOINT ["dotnet", "Bar_QR.dll"]