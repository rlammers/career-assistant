FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:ed034a8bf0b24ded0cbbac07e17825d8e9ebfe21e308191d0f7421eaf5ad4664 AS build
WORKDIR /src

COPY src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj src/backend/CareerAssistant.Api/
RUN dotnet restore src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj

COPY src/backend/CareerAssistant.Api/ src/backend/CareerAssistant.Api/
RUN dotnet publish src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:1fa23fc4872d95fd71c2833ebe65d7e84a43b2d51a31d119516852f13d9505a7 AS runtime
WORKDIR /app

RUN mkdir -p /app/data && chown -R app:app /app/data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER app

ENTRYPOINT ["dotnet", "CareerAssistant.Api.dll"]
