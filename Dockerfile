FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Bar_QR/Bar_QR.csproj", "Bar_QR/"]
RUN dotnet restore "Bar_QR/Bar_QR.csproj"

COPY . .
WORKDIR /src/Bar_QR
ARG CACHEBUST=1
RUN dotnet publish "Bar_QR.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /data/keys && chown -R app:app /data

USER app

ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ConnectionStrings__Sqlite="Data Source=/data/barqr.db"

EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet Bar_QR.dll"]
