FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Missions.Api/Missions.Api.csproj", "Missions.Api/"]
COPY ["Missions.Application/Missions.Application.csproj", "Missions.Application/"]
COPY ["Missions.Domain/Missions.Domain.csproj", "Missions.Domain/"]
COPY ["Missions.Infrastructure/Missions.Infrastructure.csproj", "Missions.Infrastructure/"]
RUN dotnet restore "Missions.Api/Missions.Api.csproj"

COPY . .
RUN dotnet publish "Missions.Api/Missions.Api.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "Missions.Api.dll"]
