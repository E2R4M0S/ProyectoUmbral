FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Teams.Api/Teams.Api.csproj", "Teams.Api/"]
COPY ["Teams.Application/Teams.Application.csproj", "Teams.Application/"]
COPY ["Teams.Domain/Teams.Domain.csproj", "Teams.Domain/"]
COPY ["Teams.Infrastructure/Teams.Infrastructure.csproj", "Teams.Infrastructure/"]
RUN dotnet restore "Teams.Api/Teams.Api.csproj"

COPY . .
RUN dotnet publish "Teams.Api/Teams.Api.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "Teams.Api.dll"]
