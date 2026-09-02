FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["EDCL.slnx", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/", "src/"]

WORKDIR "/src/src/Host/EDCL.Api"
RUN dotnet publish "EDCL.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
EXPOSE 8080

# Switch to non-root user for security (UID 1654 in .NET images)
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "edcl.api.dll"]
