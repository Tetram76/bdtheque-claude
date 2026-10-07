# syntax=docker/dockerfile:1

# The SDK runs on the platform of the build machine, and targets the platform of the image
# (TARGETARCH, set by BuildKit): only the native libraries of that platform are published.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Copy only the project files first: the Docker cache for `dotnet restore`
# is only invalidated by a dependency change, not a code change.
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/Bdtheque.Domain/Bdtheque.Domain.csproj src/Bdtheque.Domain/
COPY src/Bdtheque.Contracts/Bdtheque.Contracts.csproj src/Bdtheque.Contracts/
COPY src/Bdtheque.Infrastructure/Bdtheque.Infrastructure.csproj src/Bdtheque.Infrastructure/
COPY src/Bdtheque.Api/Bdtheque.Api.csproj src/Bdtheque.Api/
COPY src/Bdtheque.HealthProbe/Bdtheque.HealthProbe.csproj src/Bdtheque.HealthProbe/
RUN dotnet restore src/Bdtheque.Api/Bdtheque.Api.csproj -a $TARGETARCH \
    && dotnet restore src/Bdtheque.HealthProbe/Bdtheque.HealthProbe.csproj -a $TARGETARCH

COPY src/Bdtheque.Domain/ src/Bdtheque.Domain/
COPY src/Bdtheque.Contracts/ src/Bdtheque.Contracts/
COPY src/Bdtheque.Infrastructure/ src/Bdtheque.Infrastructure/
COPY src/Bdtheque.Api/ src/Bdtheque.Api/
COPY src/Bdtheque.HealthProbe/ src/Bdtheque.HealthProbe/
# Compiled without debugging symbols, which the image does not need (the stack traces of its logs
# keep the method names, without line numbers).
RUN dotnet publish src/Bdtheque.Api/Bdtheque.Api.csproj -c Release -a $TARGETARCH --self-contained false --no-restore \
        -p:DebugType=none -o /app \
    && dotnet publish src/Bdtheque.HealthProbe/Bdtheque.HealthProbe.csproj -c Release -a $TARGETARCH --self-contained false --no-restore \
        -p:DebugType=none -o /healthprobe

# Chiseled: neither shell nor package manager, non-root user; "extra" for ICU, which the French
# linguistic sorting and formatting require (choix-implementation.md § Internationalisation).
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app

COPY --from=build /app .
COPY --from=build /healthprobe /healthprobe

EXPOSE 8080

# Without shell nor curl in the image, the health check is a probe running on the image's .NET
# runtime (exec form, no shell needed).
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD ["dotnet", "/healthprobe/Bdtheque.HealthProbe.dll", "http://localhost:8080/health"]

ENTRYPOINT ["dotnet", "Bdtheque.Api.dll"]
