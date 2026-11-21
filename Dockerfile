# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src

# Copy all source files
COPY src/ ./src/
COPY frontend/ ./frontend/
COPY yarn.lock package.json ./
COPY .editorconfig ./
COPY Logo/ ./Logo/
# Restore dependencies
RUN dotnet restore src/Whisparr.sln -r linux-x64 -p:TreatWarningsAsErrors=false
# Build and publish self-contained for linux-x64
RUN dotnet msbuild src/Whisparr.sln \
    -p:Configuration=Release \
    -p:Platform=Posix \
    -p:SelfContained=true \
    -p:RuntimeIdentifier=linux-x64 \
    -p:PublishSingleFile=false \
    -p:PublishTrimmed=false \
    -p:TreatWarningsAsErrors=false \
    -t:PublishAllRids

# Build UI
RUN cd frontend && yarn install --frozen-lockfile --network-timeout 120000 && yarn build --env production

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Install required runtime dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    sqlite3 \
    libcurl4 \
    mediainfo \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user (UID/GID 1000 may already exist)
ARG PUID=1000
ARG PGID=1000
RUN getent group ${PGID} >/dev/null 2>&1 || groupadd -g ${PGID} whisparr && \
    id -u whisparr >/dev/null 2>&1 || useradd -u ${PUID} -g ${PGID} -d /config -s /bin/bash whisparr 2>/dev/null || \
    usermod -u ${PUID} -g ${PGID} -d /config -s /bin/bash whisparr 2>/dev/null || true && \
    mkdir -p /config /data && \
    chown -R ${PUID}:${PGID} /config /data

# Copy published app from builder
COPY --from=builder /src/_output/net10.0/linux-x64/publish /app
COPY --from=builder /src/_output/Whisparr.Update/net10.0/linux-x64/publish /app/Whisparr.Update
COPY --from=builder /src/_output/UI /app/UI
COPY --from=builder /src/LICENSE /app/

# Set permissions
RUN chown -R ${PUID}:${PGID} /app && \
    chmod +x /app/Whisparr /app/Whisparr.Update/Whisparr.Update

USER ${PUID}:${PGID}

WORKDIR /app
VOLUME ["/config", "/data"]
EXPOSE 6969

ENV ASPNETCORE_URLS=http://+:6969
ENV WHISPARR_DATA_DIR=/config

ENTRYPOINT ["/app/Whisparr", "-nobrowser", "-data=/config"]