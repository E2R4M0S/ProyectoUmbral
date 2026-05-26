FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Sessions.Api/Sessions.Api.csproj", "Sessions.Api/"]
COPY ["Sessions.Application/Sessions.Application.csproj", "Sessions.Application/"]
COPY ["Sessions.Domain/Sessions.Domain.csproj", "Sessions.Domain/"]
COPY ["Sessions.Infrastructure/Sessions.Infrastructure.csproj", "Sessions.Infrastructure/"]
RUN dotnet restore "Sessions.Api/Sessions.Api.csproj"

COPY . .
RUN dotnet publish "Sessions.Api/Sessions.Api.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "Sessions.Api.dll"]
