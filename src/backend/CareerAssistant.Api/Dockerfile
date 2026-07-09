FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj src/backend/CareerAssistant.Api/
RUN dotnet restore src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj

COPY src/backend/CareerAssistant.Api/ src/backend/CareerAssistant.Api/
RUN dotnet publish src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/data && chown -R app:app /app/data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER app

ENTRYPOINT ["dotnet", "CareerAssistant.Api.dll"]
