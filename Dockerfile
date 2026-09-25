# Multi-stage build for Scroll Guardian .NET 8 Backend API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and csproj files for layer caching
COPY backend/ScrollGuardian.sln backend/
COPY backend/src/ScrollGuardian.Domain/ScrollGuardian.Domain.csproj backend/src/ScrollGuardian.Domain/
COPY backend/src/ScrollGuardian.Application/ScrollGuardian.Application.csproj backend/src/ScrollGuardian.Application/
COPY backend/src/ScrollGuardian.Infrastructure/ScrollGuardian.Infrastructure.csproj backend/src/ScrollGuardian.Infrastructure/
COPY backend/src/ScrollGuardian.Api/ScrollGuardian.Api.csproj backend/src/ScrollGuardian.Api/
COPY backend/tests/ScrollGuardian.UnitTests/ScrollGuardian.UnitTests.csproj backend/tests/ScrollGuardian.UnitTests/
COPY backend/tests/ScrollGuardian.IntegrationTests/ScrollGuardian.IntegrationTests.csproj backend/tests/ScrollGuardian.IntegrationTests/

RUN dotnet restore backend/ScrollGuardian.sln

# Copy source code and build
COPY backend/ backend/
WORKDIR /app/backend/src/ScrollGuardian.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000

ENTRYPOINT ["dotnet", "ScrollGuardian.Api.dll"]
