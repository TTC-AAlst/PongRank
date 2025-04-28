FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /source

COPY *.sln .
COPY src/PongRank.WebApi/*.csproj ./src/PongRank.WebApi/
COPY src/PongRank.Frenoy/*.csproj ./src/PongRank.Frenoy/
COPY src/PongRank.DataAccess/*.csproj ./src/PongRank.DataAccess/
COPY src/PongRank.DataEntities/*.csproj ./src/PongRank.DataEntities/
COPY src/PongRank.Model/*.csproj ./src/PongRank.Model/

RUN dotnet restore

COPY src/PongRank.DataEntities/. ./src/PongRank.DataEntities/
COPY src/PongRank.DataAccess/. ./src/PongRank.DataAccess/
COPY src/PongRank.Model/. ./src/PongRank.Model/
COPY src/PongRank.Frenoy/. ./src/PongRank.Frenoy/
COPY src/PongRank.WebApi/. ./src/PongRank.WebApi/

WORKDIR /source/src/PongRank.WebApi
RUN dotnet publish -c Release -o /app



FROM mcr.microsoft.com/dotnet/aspnet:8.0 as runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT [ "dotnet", "/app/PongRank.WebApi.dll" ]
