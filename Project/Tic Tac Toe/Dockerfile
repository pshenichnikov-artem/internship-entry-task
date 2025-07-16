FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

ENV Game__BoardSize=3
ENV Game__WinConditionLength=3
ENV ConnectionStrings__DefaultConnection=""
ENV Jwt__Key=""
ENV Jwt__Issuer=""
ENV Jwt__Audience=""

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY . .

RUN dotnet restore "Tic Tac Toe/Tic Tac Toe.csproj"

WORKDIR "/src/Tic Tac Toe"
RUN dotnet build "Tic Tac Toe.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Tic Tac Toe.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*

COPY ["Tic Tac Toe/entrypoint.sh", "/app/entrypoint.sh"]
RUN chmod +x /app/entrypoint.sh

ENTRYPOINT ["/app/entrypoint.sh"]
