# syntax=docker/dockerfile:1.4  # Specify Dockerfile syntax version for BuildKit features

# --- STAGE 1: Build the application ---
# Use the SDK image for building. Alpine is preferred for smaller image size.
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

# Set the working directory inside the container for the source code
WORKDIR /src

# Copy the .csproj file first to leverage Docker's build cache.
# This ensures that if only application code changes, but dependencies don't,
# Docker can reuse the 'dotnet restore' layer, speeding up subsequent builds.
COPY ["AuthService/AuthService.csproj", "AuthService/"]

# Restore NuGet packages. Use --no-cache-dir for Alpine to save space.
RUN dotnet restore "AuthService/AuthService.csproj" --no-cache-dir

# Copy the rest of the application code
COPY . .

# Change to the project directory for building
WORKDIR "/src/AuthService"

# Publish the application for release.
# -c Release: Build in Release configuration.
# -o /app/publish: Output the published application to /app/publish.
# --no-restore: Skip restoring packages again as they were restored in an earlier step.
# --self-contained true: Embed the .NET runtime with the application, making the final image independent of a separate ASP.NET runtime image.
# -p:PublishTrimmed=true: Trim unused framework assemblies to reduce size.
# -p:PublishReadyToRun=true: Ahead-of-time (AOT) compilation for faster startup.
# -p:IncludeNativeLibrariesForSelfExtract=true: Ensures native libraries are included (relevant for self-contained).
# /p:UseAppHost=false: Creates a DLL instead of an executable, suitable for 'dotnet AuthService.dll' entrypoint.
RUN dotnet publish "AuthService.csproj" -c Release -o /app/publish \
    --no-restore \
    --self-contained true \
    -p:PublishTrimmed=true \
    -p:PublishReadyToRun=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    /p:UseAppHost=false

# --- STAGE 2: Create the final runtime image ---
# Use a minimal base image like Alpine or distroless.
# Since we used --self-contained true in publish, we just need a very basic OS layer.
# Alpine is a common choice for tiny base images, but you could also consider:
# FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine AS final # if only runtime dependencies are needed
FROM alpine:3.20 AS final

# Install curl for HEALTHCHECK. This needs to be done *before* switching to non-root user
# and will add a small amount to the image size.
# Ensure you clean up the apk cache afterwards to keep the image minimal.
RUN apk add --no-cache curl

# Set the working directory for the application
WORKDIR /app

# Expose the port your application listens on.
# ASP.NET Core defaults to 8080 in containers. Only expose what's necessary.
EXPOSE 8080

# Create a dedicated non-root user and group for security.
# Using fixed IDs (e.g., 10001) avoids conflicts and ensures consistency.
RUN addgroup --gid 10001 appgroup \
    && adduser --uid 10001 --ingroup appgroup --disabled-password --gecos "" appuser \
    # Ensure the /app directory is owned by the new user so appuser can read/execute
    && chown -R appuser:appgroup /app

# Copy the published application from the build stage into the final image
COPY --from=build /app/publish .

# Switch to the non-root user for running the application.
# This is a critical security measure.
USER appuser

# Define a health check. This allows container orchestrators (like Kubernetes)
# to determine if your application is truly healthy and responsive.
# Adjust the endpoint (/healthz) based on your actual health check implementation.
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
  CMD curl --fail http://localhost:8080/healthz || exit 1

# Command to run the application when the container starts.
# Use ENTRYPOINT for the primary executable.
ENTRYPOINT ["./AuthService"] # Using ./AuthService if self-contained and AppHost=true (default for self-contained)
                             # If UseAppHost=false (as in the build stage), use ["dotnet", "AuthService.dll"]

# Metadata labels for the image. These provide useful information and are
# standardized by Open Containers Initiative (OCI).
LABEL org.opencontainers.image.source="https://github.com/your-org/your-repo-for-authservice" \
      org.opencontainers.image.licenses="MIT" \
      org.opencontainers.image.vendor="YourCompany" \
      org.opencontainers.image.title="AuthService Minimal API" \
      org.opencontainers.image.version="1.0.0" \
      org.opencontainers.image.description="Authentication and Authorization Minimal API Service for your application."