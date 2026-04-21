############################################################
# 0. Global build arguments
############################################################
# pass default value for argument to avoid build-time errors, can be overridden at runtime
ARG APP_DLL=docke_web_Api.dll  

# pass default value for argument to avoid build-time errors, can be overridden at runtime
ARG APP_PORT=8080 

############################################################
# 1. Base runtime image
############################################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# ARG is scope based and must therefore be re-declared in this stage
ARG APP_PORT  

# Create non-root user (K8s best practice)
RUN useradd \
    --uid 1000 \
    --create-home \
    --shell /sbin/nologin \
    appuser

WORKDIR /repo

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

WORKDIR /repo

# Copy everything (use .dockerignore to keep this lean)
COPY . .

#RUN echo "printing repo image tree structure" && ls -la /repo
#RUN echo "printing src image tree structure" && ls -la /repo/src
#RUN echo "printing nuget-config image tree structure" && ls -la /repo/nuget-config

# Restore & build
RUN dotnet restore /repo/src/docke_web_Api.csproj --configfile /repo/nuget-config/NuGet.config

# explicitly switch off restore otherwise it will try to restore again and fail due to missing credentials (since we won't have access to the secret at build time)
RUN dotnet publish /repo/src/docke_web_Api.csproj -c Release -o /repo/publish --no-restore /p:UseAppHost=false

############################################################
# 3. Final runtime image
############################################################
FROM base AS final

# ARG is scope based and must therefore be re-declared in this stage
ARG APP_DLL 

WORKDIR /repo

# Writable directory for runtime data
ENV WRITE_DIR="/repo/data" \
    APP_DLL="${APP_DLL}"

RUN mkdir -p ${WRITE_DIR} && \
    chown -R appuser:appuser /repo

# Copy published files
COPY --from=build /repo/publish .

# Safety check: ensure DLL exists
RUN test -f "/repo/${APP_DLL}"

# Kubernetes-friendly runtime user
USER appuser

# ENTRYPOINT required for K8s
ENTRYPOINT ["sh", "-c", "dotnet /repo/${APP_DLL}"]

# CMD can be overridden by K8s args
CMD ["--write-dir", "/repo/data", "--AWSSecretName", "docke_web_api_k8s", "--AWSRegion", "us-east-1"]