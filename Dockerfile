FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Bar_QR/Bar_QR.csproj", "Bar_QR/"]
RUN dotnet restore "Bar_QR/Bar_QR.csproj"

COPY . .
WORKDIR /src/Bar_QR
RUN dotnet publish "Bar_QR.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ConnectionStrings__Sqlite="Data Source=/app/barqr.db"

EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet Bar_QR.dll"]
