FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AccountLedger.sln ./
COPY src/AccountLedger.Api/AccountLedger.Api.csproj src/AccountLedger.Api/
COPY src/AccountLedger.Application/AccountLedger.Application.csproj src/AccountLedger.Application/
COPY src/AccountLedger.Domain/AccountLedger.Domain.csproj src/AccountLedger.Domain/
COPY src/AccountLedger.Infrastructure/AccountLedger.Infrastructure.csproj src/AccountLedger.Infrastructure/

RUN dotnet restore src/AccountLedger.Api/AccountLedger.Api.csproj

COPY . ./
RUN dotnet publish src/AccountLedger.Api/AccountLedger.Api.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "AccountLedger.Api.dll"]
