FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["RentalApp.Api/RentalApp.Api.csproj", "RentalApp.Api/"]
COPY ["RentalApp.Contracts/RentalApp.Contracts.csproj", "RentalApp.Contracts/"]
COPY ["RentalApp.Database/RentalApp.Database.csproj", "RentalApp.Database/"]
COPY ["RentalApp.Migrations/RentalApp.Migrations.csproj", "RentalApp.Migrations/"]
RUN dotnet restore "RentalApp.Api/RentalApp.Api.csproj"
COPY . .
RUN dotnet publish "RentalApp.Api/RentalApp.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "RentalApp.Api.dll"]
