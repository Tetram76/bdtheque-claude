# Site of the technical documentation of the validated commit, served by the validation environment on the machine of
# the validation only (choix-implementation/documentation-technique.md). Never published: built on that machine, like the
# images of the pull request.
FROM mcr.microsoft.com/dotnet/sdk:10.0.404 AS build
WORKDIR /src

# DocFX, at the version of the repository (dotnet-tools.json): restored in a layer of its own, kept while it does not change.
COPY dotnet-tools.json ./
RUN dotnet tool restore \
    && cp -r "$(ls -d /root/.nuget/packages/docfx/*/tools/net10.0/any)" /docfx

COPY scripts/Build-TechnicalDocumentation.ps1 scripts/
COPY docs/tech/ docs/tech/
# The commit validated, named at the foot of each page and in its links to GitHub: the sources come without their history.
ARG SOURCE_COMMIT=unknown
RUN pwsh -NoLogo -NoProfile -File scripts/Build-TechnicalDocumentation.ps1 -Output /site -Commit "$SOURCE_COMMIT"

# The runtime image of `api`, already on the machine: no other base image to pull nor to keep up to date. DocFX serves
# the static site.
FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-resolute-chiseled-extra AS final
COPY --from=build /docfx /docfx
COPY --from=build /site /site
EXPOSE 8080
ENTRYPOINT ["dotnet", "/docfx/docfx.dll", "serve", "/site", "--hostname", "*", "--port", "8080"]
