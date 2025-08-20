FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER 1654
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar os .csproj de todos os projetos
COPY ["MDS.Scraper/MDS.Scraper.csproj", "MDS.Scraper/"]
COPY ["MDS.Data/MDS.Data.csproj", "MDS.Data/"]
COPY ["MeDeixaSaber.Core/MeDeixaSaber.Core.csproj", "MeDeixaSaber.Core/"]

# Restaurar pacotes
RUN dotnet restore "MDS.Scraper/MDS.Scraper.csproj"

# Copiar tudo para dentro do container
COPY . .

WORKDIR "/src/MDS.Scraper"
RUN dotnet build "MDS.Scraper.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/MDS.Scraper"
RUN dotnet publish "MDS.Scraper.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MDS.Scraper.dll"]
