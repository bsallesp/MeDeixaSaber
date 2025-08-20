FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MDS.Runner.NewsLlm/MDS.Runner.NewsLlm.csproj", "MDS.Runner.NewsLlm/"]
RUN dotnet restore "MDS.Runner.NewsLlm/MDS.Runner.NewsLlm.csproj"
COPY . .
WORKDIR "/src/MDS.Runner.NewsLlm"
RUN dotnet build "./MDS.Runner.NewsLlm.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MDS.Runner.NewsLlm.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MDS.Runner.NewsLlm.dll"]
