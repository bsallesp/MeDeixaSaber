# ---------------------------
# Base (runtime)
# ---------------------------
FROM mcr.microsoft.com/dotnet/runtime:9.0-noble AS base
USER 1654
WORKDIR /app

# ---------------------------
# Build (SDK)
# ---------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copia csproj (cache de restore)
COPY ["MDS.Runner.Scraper/MDS.Runner.Scraper.csproj", "MDS.Runner.Scraper/"]
COPY ["MDS.Data/MDS.Data.csproj", "MDS.Data/"]
COPY ["MeDeixaSaber.Core/MeDeixaSaber.Core.csproj", "MeDeixaSaber.Core/"]

RUN dotnet restore "MDS.Runner.Scraper/MDS.Runner.Scraper.csproj"

# Copia restante do código
COPY . .
WORKDIR "/src/MDS.Runner.Scraper"
RUN dotnet build "MDS.Runner.Scraper.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# ---------------------------
# Publish
# ---------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/MDS.Runner.Scraper"
RUN dotnet publish "MDS.Runner.Scraper.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# ---------------------------
# Final (runtime)
# ---------------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MDS.Runner.Scraper.dll"]
