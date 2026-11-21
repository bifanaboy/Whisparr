# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src

# Copy project files for restore caching
COPY src/*.sln .
COPY src/NzbDrone/*.csproj src/NzbDrone/
COPY src/NzbDrone.Host/*.csproj src/NzbDrone.Host/
COPY src/NzbDrone.Core/*.csproj src/NzbDrone.Core/
COPY src/NzbDrone.Common/*.csproj src/NzbDrone.Common/
COPY src/NzbDrone.SignalR/*.csproj src/NzbDrone.SignalR/
COPY src/NzbDrone.Update/*.csproj src/NzbDrone.Update/
COPY src/NzbDrone.Mono/*.csproj src/NzbDrone.Mono/
COPY src/Whisparr.Api.V3/*.csproj src/Whisparr.Api.V3/
COPY src/Whisparr.Http/*.csproj src/Whisparr.Http/
COPY src/Directory.Build.props src/
COPY src/Directory.Build.targets src/ 2>/dev/null || true

# Restore dependencies
RUN dotnet restore src/Whisparr.sln -r linux-x64

# Copy everything and build
COPY src/ ./src/
COPY frontend/ ./frontend/
COPY yarn.lock package.json ./
COPY .editorconfig ./

# Build and publish self-contained for linux-x64
RUN dotnet msbuild src/Whisparr.sln \
    -p:Configuration=Release \
    -p:Platform=Posix \
    -p:SelfContained=true \
    -p:RuntimeIdentifier=linux-x64 \
    -p:PublishSingleFile=false \
    -p:PublishTrimmed=false \
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

# Create non-root user
ARG PUID=1000
ARG PGID=1000
RUN groupadd -g ${PGID} whisparr && \
    useradd -u ${PUID} -g ${PGID} -d /config -s /bin/bash whisparr && \
    mkdir -p /config /data && \
    chown -R whisparr:whisparr /config /data

# Copy published app from builder
COPY --from=builder /src/_output/net10.0/linux-x64/publish /app
COPY --from=builder /src/_output/Whisparr.Update/net10.0/linux-x64/publish /app/Whisparr.Update
COPY --from=builder /src/_output/UI /app/UI
COPY --from=builder /src/LICENSE /app/

# Set permissions
RUN chown -R whisparr:whisparr /app && \
    chmod +x /app/Whisparr /app/Whisparr.Update/Whisparr.Update

USER whisparr

WORKDIR /app
VOLUME ["/config", "/data"]
EXPOSE 6969

ENV ASPNETCORE_URLS=http://+:6969
ENV WHISPARR_DATA_DIR=/config

ENTRYPOINT ["/app/Whisparr", "-nobrowser", "-data=/config"]