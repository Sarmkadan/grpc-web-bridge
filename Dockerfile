# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy project files
COPY ["src/GrpcWebBridge/GrpcWebBridge.csproj", "GrpcWebBridge/"]
RUN dotnet restore "GrpcWebBridge/GrpcWebBridge.csproj"

# Copy source code
COPY src/GrpcWebBridge/ GrpcWebBridge/

# Build the project
WORKDIR "/src/GrpcWebBridge"
RUN dotnet build "GrpcWebBridge.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "GrpcWebBridge.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Create non-root user for security
RUN useradd -m appuser && \
    mkdir -p /app/logs && \
    chown -R appuser:appuser /app

# Copy published application
COPY --from=publish --chown=appuser:appuser /app/publish .

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_DETAILEDERRORS=false
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY=true

# Run the application
ENTRYPOINT ["dotnet", "GrpcWebBridge.dll"]
