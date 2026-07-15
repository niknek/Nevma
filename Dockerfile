# syntax=docker/dockerfile:1.7

ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0.301-noble
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0.9-noble

FROM ${SDK_IMAGE} AS build
WORKDIR /src
COPY . .
ARG PROJECT
RUN test -n "${PROJECT}" && \
    dotnet restore "${PROJECT}" && \
    dotnet publish "${PROJECT}" \
      --configuration Release \
      --no-restore \
      --output /app/publish \
      /p:UseAppHost=false

FROM ${SDK_IMAGE} AS migrations
WORKDIR /src
COPY . .
RUN dotnet tool restore && \
    dotnet restore Nevma.slnx && \
    dotnet build Nevma.slnx --configuration Release --no-restore
ENTRYPOINT ["bash", "scripts/migrate-all.sh"]

FROM ${RUNTIME_IMAGE} AS final
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet"]
