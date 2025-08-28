# Base (runtime)
FROM mcr.microsoft.com/dotnet/runtime:9.0-jammy AS base
USER 1654
WORKDIR /app

# Build (SDK)
FROM mcr.microsoft.com/dotnet/sdk:9.0-jammy AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["MDS.Runner.Scraper/MDS.Runner.Scraper.csproj", "MDS.Runner.Scraper/"]
COPY ["MDS.Data/MDS.Data.csproj", "MDS.Data/"]
COPY ["MeDeixaSaber.Core/MeDeixaSaber.Core.csproj", "MeDeixaSaber.Core/"]
RUN dotnet restore "MDS.Runner.Scraper/MDS.Runner.Scraper.csproj"

COPY . .
WORKDIR "/src/MDS.Runner.Scraper"
RUN dotnet build "MDS.Runner.Scraper.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/MDS.Runner.Scraper"
RUN dotnet publish "MDS.Runner.Scraper.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MDS.Runner.Scraper.dll"]
