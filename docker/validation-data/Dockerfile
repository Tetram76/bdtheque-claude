# syntax=docker/dockerfile:1

# Loader of the validation data, run once by the validation environment (choix-implementation/deploiement.md
# § Environnement de validation fonctionnelle). Never published: built on the machine of the validation,
# like the images of the pull request.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.402 AS build
ARG TARGETARCH
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/Bdtheque.Contracts/Bdtheque.Contracts.csproj src/Bdtheque.Contracts/
COPY src/Bdtheque.ValidationData/Bdtheque.ValidationData.csproj src/Bdtheque.ValidationData/
RUN dotnet restore src/Bdtheque.ValidationData/Bdtheque.ValidationData.csproj -a $TARGETARCH

COPY src/Bdtheque.Contracts/ src/Bdtheque.Contracts/
COPY src/Bdtheque.ValidationData/ src/Bdtheque.ValidationData/
RUN dotnet publish src/Bdtheque.ValidationData/Bdtheque.ValidationData.csproj -c Release -a $TARGETARCH --self-contained false --no-restore \
        -p:DebugType=none -o /app

# The runtime image of `api`, already on the machine: no other base image to pull nor to keep up to date.
FROM mcr.microsoft.com/dotnet/aspnet:10.0.13-noble-chiseled-extra AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Bdtheque.ValidationData.dll"]
