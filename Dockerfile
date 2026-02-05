FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ["Bar_QR/Bar_QR.csproj", "Bar_QR/"]
RUN dotnet restore "Bar_QR/Bar_QR.csproj"

# copy everything else and build
COPY . .
WORKDIR "/src/Bar_QR"
RUN dotnet publish "Bar_QR.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV DOTNET_RUNNING_IN_CONTAINER=true

# Force Kestrel to listen on container port 80. Railway maps its external port to container 80.
ENV ASPNETCORE_URLS=http://0.0.0.0:80
EXPOSE 80

ENTRYPOINT ["dotnet", "Bar_QR.dll"]
