# Build context: src/backend/TixFlow (see docker-compose.yml)
# One image, two roles: the api container serves HTTP, the workers container runs the background services.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TixFlow.Domain/TixFlow.Domain.csproj TixFlow.Domain/
COPY TixFlow.Infrastructure/TixFlow.Infrastructure.csproj TixFlow.Infrastructure/
COPY TixFlow.Workers/TixFlow.Workers.csproj TixFlow.Workers/
COPY TixFlow.Api/TixFlow.Api.csproj TixFlow.Api/
RUN dotnet restore TixFlow.Api/TixFlow.Api.csproj
COPY TixFlow.Domain/ TixFlow.Domain/
COPY TixFlow.Infrastructure/ TixFlow.Infrastructure/
COPY TixFlow.Workers/ TixFlow.Workers/
COPY TixFlow.Api/ TixFlow.Api/
RUN dotnet publish TixFlow.Api/TixFlow.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# Npgsql probes for Kerberos at startup and logs an error when the library is missing; curl is for healthchecks.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "TixFlow.Api.dll"]
