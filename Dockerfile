# Shifa API image (ADR-017). Build from the repository root:
#   docker build -t shifa-api .
# Runs as a non-root user on port 8080; state lives in /data (key ring + attachments): mount a persistent volume there.

FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
WORKDIR /src
# Restore first (cached until a project file or package version changes).
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY Core/Core.csproj Core/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY Shifa/Shifa.csproj Shifa/
RUN dotnet restore Shifa/Shifa.csproj
COPY Core/ Core/
COPY Infrastructure/ Infrastructure/
COPY Shifa/ Shifa/
RUN dotnet publish Shifa/Shifa.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.14 AS runtime
# fonts-noto-core: Noto Naskh Arabic for PDF reports (QuestPDF draws only fonts it can find, ADR-016).
# libfontconfig1: font discovery for QuestPDF. tzdata: branch time zones (Africa/Cairo).
RUN apt-get update \
    && apt-get install -y --no-install-recommends fonts-noto-core libfontconfig1 tzdata \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /data/keys /data/files \
    && chown -R $APP_UID /data
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DataProtection__KeysPath=/data/keys \
    FileStorage__RootPath=/data/files \
    Reports__UseSystemFonts=true \
    Reports__PdfFontFamilies="Noto Naskh Arabic"

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Shifa.dll"]
