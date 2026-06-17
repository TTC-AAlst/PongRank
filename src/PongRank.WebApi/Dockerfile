FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

ARG Nuget_CustomFeedUserName
ARG Nuget_CustomFeedPassword

COPY *.slnx .
COPY nuget.config .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY src/PongRank.Frenoy/*.csproj ./src/PongRank.Frenoy/
COPY src/PongRank.DataAccess/*.csproj ./src/PongRank.DataAccess/
COPY src/PongRank.DataEntities/*.csproj ./src/PongRank.DataEntities/
COPY src/PongRank.Model/*.csproj ./src/PongRank.Model/
COPY src/PongRank.ML/*.csproj ./src/PongRank.ML/
COPY src/PongRank.WebApi/*.csproj ./src/PongRank.WebApi/
COPY src/PongRank.ConsoleApp/*.csproj ./src/PongRank.ConsoleApp/

RUN dotnet restore src/PongRank.WebApi/PongRank.WebApi.csproj

COPY src/PongRank.Frenoy/. ./src/PongRank.Frenoy/
COPY src/PongRank.DataAccess/. ./src/PongRank.DataAccess/
COPY src/PongRank.DataEntities/. ./src/PongRank.DataEntities/
COPY src/PongRank.Model/. ./src/PongRank.Model/
COPY src/PongRank.ML/. ./src/PongRank.ML/
COPY src/PongRank.WebApi/. ./src/PongRank.WebApi/
COPY src/PongRank.ConsoleApp/. ./src/PongRank.ConsoleApp/

WORKDIR /source/src/PongRank.WebApi
RUN dotnet publish -c Release -o /app


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# libgomp1: LightGBM (ML.NET) OpenMP runtime. libgssapi-krb5-2: Npgsql 10 loads
# libgssapi_krb5.so.2 during connection auth; absent from the slim aspnet image.
RUN apt-get update && apt-get install -y libgomp1 libgssapi-krb5-2 curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .

ENTRYPOINT [ "dotnet", "/app/PongRank.WebApi.dll" ]
