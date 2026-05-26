FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Trivia.Api/Trivia.Api.csproj", "Trivia.Api/"]
COPY ["Trivia.Application/Trivia.Application.csproj", "Trivia.Application/"]
COPY ["Trivia.Domain/Trivia.Domain.csproj", "Trivia.Domain/"]
COPY ["Trivia.Infrastructure/Trivia.Infrastructure.csproj", "Trivia.Infrastructure/"]
RUN dotnet restore "Trivia.Api/Trivia.Api.csproj"

COPY . .
RUN dotnet publish "Trivia.Api/Trivia.Api.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "Trivia.Api.dll"]
