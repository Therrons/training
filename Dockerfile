###############################################
# 0. Build args (can be overridden at build time)
###############################################
ARG dll="docke_web_Api.dll"   # Assembly name produced by publish
ARG location="."              # Not needed in this flow, but kept for compatibility

###############################################
# 1. Base ASP.NET Runtime
###############################################
                                                  
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /repo

#COPY . .  // Don't copy everything in the base image, just the certs if needed. The rest will be copied in the build stage.

RUN rm -rf /repo/.vs
RUN rm -rf /repo/src/.vs

# RUN echo "printing image tree structure" && ls -la /repo

# Optional: custom CA certificates (folder may be empty)
COPY certs/ /usr/local/share/ca-certificates/
RUN update-ca-certificates || true

# ASP.NET configuration
EXPOSE 8080

ENV ASPNETCORE_URLS="http://0.0.0.0:8080" \
    ASPNETCORE_ENVIRONMENT="LOC"
                                  

###############################################
# 2. Build Stage (SDK)
###############################################
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /repo

COPY . .
#
#RUN rm -rf /repo/.vs
#RUN rm -rf /repo/src/.vs

# Make sure CA store is present/updated (useful on corp networks)
RUN apt-get update && \
    apt-get install -y --no-install-recommends ca-certificates && \
    update-ca-certificates && \
    rm -rf /var/lib/apt/lists/*

#RUN echo "printing repo image tree structure" && ls -la /repo
#RUN echo "printing src image tree structure" && ls -la /repo/src
#RUN echo "printing nuget-config image tree structure" && ls -la /repo/nuget-config

RUN dotnet restore ./src/docke_web_Api.csproj --configfile ./nuget-config/NuGet.config # -v diagnostic

RUN dotnet build /repo/src/docke_web_Api.csproj -c Release -o /repo/build

###############################################
# 3. Publish Stage
###############################################

FROM build AS publish
RUN dotnet publish /repo/src/docke_web_Api.csproj \
    -c Release \
    -o /repo/publish \
    /p:UseAppHost=false

###############################################
# 4. Final Runtime Image
###############################################
FROM base AS final


# Install curl in the final runtime image
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*


# Bring in a copy of the DLL name to the final stage scope
ARG dll="docke_web_Api.dll"

WORKDIR /repo

# Set environment variables with LITERAL values for the application to use at runtime
ENV WRITE_DIR="/repo/data"
ENV DLL_PATH="/repo/${dll}"
ENV Build_Version="1.0.0"
ENV AWSSecretName="docke_web_api_k8s"
ENV AWSRegion="us-east-1"

# Create a directory, specify -p in case the parent directories 
# don't exist. if it does exist, it won't throw an error and will just continue.
RUN mkdir -p "${WRITE_DIR}"

# Change ownership of WRITE_DIR to the first non-root user (1000)
RUN chown -R 1000:1000 "${WRITE_DIR}"

# Declare the volume for the write directory (can be overridden at runtime) 
# this will overwrite the above directory if it exists but the permission has been set,
VOLUME ["/repo/data"]

COPY --from=publish /repo/publish .

# Optional safety check: ensure the DLL exists after publish copy
RUN if [ ! -f "/repo/${dll}" ]; then \
      echo "ERROR: /repo/${dll} not found. Check your <AssemblyName> or project output."; \
      ls -al /repo; \
      exit 1; \
    fi

# Run the app
ENTRYPOINT ["dotnet", "/repo/docke_web_Api.dll"]

# supply arg with LITERAL values to the the program.cs Main method
# CMD ["--arg1", "value1", "--arg2", "value2"]
CMD ["--write-dir", "/repo/data","--AWSSecretName", "docke_web_api_k8s","--AWSRegion", "us-east-1"] 