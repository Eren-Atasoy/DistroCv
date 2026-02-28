# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/DistroCv.Api/DistroCv.Api.csproj", "src/DistroCv.Api/"]
COPY ["src/DistroCv.Core/DistroCv.Core.csproj", "src/DistroCv.Core/"]
COPY ["src/DistroCv.Infrastructure/DistroCv.Infrastructure.csproj", "src/DistroCv.Infrastructure/"]
RUN dotnet restore "src/DistroCv.Api/DistroCv.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/DistroCv.Api"
RUN dotnet build "DistroCv.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DistroCv.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
# Note: curl might need to be installed in the final image if not present
# For now, we'll keep it as is from your original file
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "DistroCv.Api.dll"]
