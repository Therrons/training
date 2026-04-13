############################################################
# 0. Global build arguments
############################################################
ARG APP_DLL=docke_web_Api.dll
ARG APP_PORT=8080

############################################################
# 1. Base runtime image
############################################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Create non-root user (K8s best practice)
RUN useradd \
    --uid 1000 \
    --create-home \
    --shell /sbin/nologin \
    appuser

WORKDIR /app

# Optional: custom CA certificates (folder may be empty but must exist)
# If you don't use custom certs, you can delete this block safely
COPY certs/ /usr/local/share/ca-certificates/
RUN update-ca-certificates || true

# ASP.NET Core configuration
ENV ASPNETCORE_URLS="http://0.0.0.0:${APP_PORT}" \
    ASPNETCORE_ENVIRONMENT="LOC" \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE ${APP_PORT}

############################################################
# 2. Build stage
############################################################
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy everything (use .dockerignore to keep this lean)
COPY . .

#RUN echo "printing app image tree structure" && ls -la /app
#RUN echo "printing src image tree structure" && ls -la /app/src
#RUN echo "printing nuget-config image tree structure" && ls -la /app/nuget-config

# Restore & build
RUN dotnet restore ./src/docke_web_Api.csproj --configfile ./nuget-config/NuGet.config

RUN dotnet publish ./src/docke_web_Api.csproj -c Release -o /app/publish /p:UseAppHost=false

############################################################
# 3. Final runtime image
############################################################
FROM base AS final

ARG APP_DLL

WORKDIR /app

# Writable directory for runtime data
ENV WRITE_DIR="/app/data" \
    APP_DLL="${APP_DLL}"

RUN mkdir -p ${WRITE_DIR} && \
    chown -R appuser:appuser /app

# Copy published files
COPY --from=build /app/publish .

# Safety check: ensure DLL exists
RUN test -f "/app/${APP_DLL}"

# Kubernetes-friendly runtime user
USER appuser

# ENTRYPOINT required for K8s
ENTRYPOINT ["sh", "-c", "dotnet /app/${APP_DLL}"]

# CMD can be overridden by K8s args
CMD ["--write-dir", "/app/data", "--AWSSecretName", "docke_web_api_k8s", "--AWSRegion", "us-east-1"]