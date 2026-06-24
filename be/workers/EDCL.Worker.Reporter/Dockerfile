FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["EDCL.slnx", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/", "src/"]
COPY ["workers/", "workers/"]

WORKDIR "/src/workers/EDCL.Worker.Reporter"
RUN dotnet publish "EDCL.Worker.Reporter.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS runtime
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EDCL.Worker.Reporter.dll"]
