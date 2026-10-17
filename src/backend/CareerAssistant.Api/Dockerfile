ARG SOURCE_REVISION=local
ARG SOURCE_URL=local

FROM mcr.microsoft.com/dotnet/sdk:10.0.402@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa AS build
WORKDIR /src

COPY src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj src/backend/CareerAssistant.Api/
RUN dotnet restore src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj

COPY src/backend/CareerAssistant.Api/ src/backend/CareerAssistant.Api/
RUN dotnet publish src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0.13@sha256:d5633778c754e89a386882eddf8dbee6942447a8a4b8267c19cf1e1158d0b0ac AS runtime
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
