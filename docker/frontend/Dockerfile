# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/Bdtheque.Contracts/Bdtheque.Contracts.csproj src/Bdtheque.Contracts/
COPY src/Bdtheque.Frontend/Bdtheque.Frontend.csproj src/Bdtheque.Frontend/
RUN dotnet restore src/Bdtheque.Frontend/Bdtheque.Frontend.csproj

COPY src/Bdtheque.Contracts/ src/Bdtheque.Contracts/
COPY src/Bdtheque.Frontend/ src/Bdtheque.Frontend/
RUN dotnet publish src/Bdtheque.Frontend/Bdtheque.Frontend.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

USER $APP_UID
ENTRYPOINT ["dotnet", "Bdtheque.Frontend.dll"]
