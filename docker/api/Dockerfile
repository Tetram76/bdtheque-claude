# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first: the Docker cache for `dotnet restore`
# is only invalidated by a dependency change, not a code change.
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/Bdtheque.Domain/Bdtheque.Domain.csproj src/Bdtheque.Domain/
COPY src/Bdtheque.Contracts/Bdtheque.Contracts.csproj src/Bdtheque.Contracts/
COPY src/Bdtheque.Infrastructure/Bdtheque.Infrastructure.csproj src/Bdtheque.Infrastructure/
COPY src/Bdtheque.Api/Bdtheque.Api.csproj src/Bdtheque.Api/
RUN dotnet restore src/Bdtheque.Api/Bdtheque.Api.csproj

COPY src/Bdtheque.Domain/ src/Bdtheque.Domain/
COPY src/Bdtheque.Contracts/ src/Bdtheque.Contracts/
COPY src/Bdtheque.Infrastructure/ src/Bdtheque.Infrastructure/
COPY src/Bdtheque.Api/ src/Bdtheque.Api/
RUN dotnet publish src/Bdtheque.Api/Bdtheque.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is required by the HEALTHCHECK below; installed before switching to the non-root user.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

USER $APP_UID
ENTRYPOINT ["dotnet", "Bdtheque.Api.dll"]
