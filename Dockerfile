FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Bar.Eugenio/Bar.Eugenio.csproj", "Bar.Eugenio/"]
RUN dotnet restore "Bar.Eugenio/Bar.Eugenio.csproj"

COPY . .
WORKDIR /src/Bar.Eugenio
RUN dotnet publish "Bar.Eugenio.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN mkdir -p /data

COPY --from=build /app/publish .

ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_URLS=http://+:${PORT}
ENV ConnectionStrings__DefaultConnection="Data Source=/data/app.db"

EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT} dotnet Bar.Eugenio.dll"]
