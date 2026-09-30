ARG SOURCE_REVISION=local
ARG SOURCE_URL=local

FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:35d40304542c8689331f8cab17c65926cdf48fe711e289321d71924b230a7d29 AS build
WORKDIR /src

COPY src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj src/backend/CareerAssistant.Api/
RUN dotnet restore src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj

COPY src/backend/CareerAssistant.Api/ src/backend/CareerAssistant.Api/
RUN dotnet publish src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:2d584d8147faddb0d678c5748d47953e5b8e18621ed4fb7049a91381d9d7746f AS runtime
ARG SOURCE_REVISION
ARG SOURCE_URL
WORKDIR /app

LABEL org.opencontainers.image.revision="${SOURCE_REVISION}" \
      org.opencontainers.image.source="${SOURCE_URL}"

RUN apt-get update && apt-get upgrade -y && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /app/data && chown -R app:app /app/data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER app

ENTRYPOINT ["dotnet", "CareerAssistant.Api.dll"]
