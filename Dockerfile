# --- Frontend Build Stage ---
FROM node:20-alpine AS client-build
WORKDIR /app
COPY client/package*.json ./
RUN npm install
COPY client/ ./
# Relative path kullanacağımız için VITE_API_URL'e gerek yok ama /api ekine dikkat
RUN npm run build

# --- Backend Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy projects
COPY ["src/DistroCv.Api/DistroCv.Api.csproj", "src/DistroCv.Api/"]
COPY ["src/DistroCv.Core/DistroCv.Core.csproj", "src/DistroCv.Core/"]
COPY ["src/DistroCv.Infrastructure/DistroCv.Infrastructure.csproj", "src/DistroCv.Infrastructure/"]
RUN dotnet restore "src/DistroCv.Api/DistroCv.Api.csproj"

COPY . .
# Frontend build sonuçlarını static file olarak wwwroot'a taşı
COPY --from=client-build /app/dist ./src/DistroCv.Api/wwwroot

WORKDIR "/src/src/DistroCv.Api"
RUN dotnet publish "DistroCv.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

RUN groupadd -r appuser && useradd -r -g appuser appuser
COPY --from=build /app/publish .
RUN chown -R appuser:appuser /app

USER appuser
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "DistroCv.Api.dll"]
