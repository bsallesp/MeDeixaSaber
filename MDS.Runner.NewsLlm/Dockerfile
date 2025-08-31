FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MDS.Runner.NewsLlm/MDS.Runner.NewsLlm.csproj", "MDS.Runner.NewsLlm/"]
COPY ["MDS.Data/MDS.Data.csproj", "MDS.Data/"]
COPY ["MDS.Infrastructure/MDS.Infrastructure.csproj", "MDS.Infrastructure/"]
COPY ["MDS.Application/MDS.Application.csproj", "MDS.Application/"]
COPY ["MeDeixaSaber.Core/MeDeixaSaber.Core.csproj", "MeDeixaSaber.Core/"]
RUN dotnet restore "MDS.Runner.NewsLlm/MDS.Runner.NewsLlm.csproj"
COPY . .
WORKDIR /src/MDS.Runner.NewsLlm
RUN dotnet publish -c $BUILD_CONFIGURATION -r linux-x64 --self-contained true -o /app/publish /p:PublishSingleFile=true /p:UseAppHost=true

FROM mcr.microsoft.com/dotnet/runtime-deps:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV DOTNET_EnableDiagnostics=0
ENTRYPOINT ["./MDS.Runner.NewsLlm"]
