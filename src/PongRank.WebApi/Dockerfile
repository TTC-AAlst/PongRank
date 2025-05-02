FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Upgrade to 9.0?

#RUN cmake --version
#
#RUN apt-get update && apt-get upgrade
#RUN apt-get install git clang-3.9 libunwind8 curl libomp-dev -y 
#

# https://github.com/AMS21/docker-cmake/blob/main/Dockerfile
#RUN apk update && apk upgrade --no-cache && \
    #apk add --no-cache gcc g++ make git linux-headers openssl-dev && \
    #git clone --depth 1 --branch "${VERSION}" https://github.com/Kitware/CMake.git && \
    #cd CMake && \
    #if [[ -d "../patches/${VERSION}" ]]; then git apply ../patches/${VERSION}/*.patch; fi && \
    #CFLAGS="-w -O3 -flto -pipe -Wno-error=incompatible-pointer-types" \
    #CXXFLAGS="-w -O3 -flto -pipe -Wno-error=incompatible-pointer-types" \
    #./bootstrap --parallel=$(nproc) && make -j $(nproc) && \
    #make install && \
    #cd .. && \
    #rm -rf CMake && rm -rf patches && \
    #rm -rf /usr/local/share/cmake-*/Help && \
    #apk del --purge --no-cache git linux-headers openssl-dev && \
    #rm -rf /var/cache/apk/*
#
#RUN cmake --version

#RUN git clone --recursive https://github.com/microsoft/LightGBM.git && \
    #cd LightGBM && mkdir build && cd build && \
    #cmake .. && make -j && make install

# CMake 3.28 or higher is required.  You are running version 3.25.1

WORKDIR /source

COPY *.sln .
COPY src/PongRank.Frenoy/*.csproj ./src/PongRank.Frenoy/
COPY src/PongRank.DataAccess/*.csproj ./src/PongRank.DataAccess/
COPY src/PongRank.DataEntities/*.csproj ./src/PongRank.DataEntities/
COPY src/PongRank.Model/*.csproj ./src/PongRank.Model/
COPY src/PongRank.ML/*.csproj ./src/PongRank.ML/
COPY src/PongRank.WebApi/*.csproj ./src/PongRank.WebApi/
COPY src/PongRank.ConsoleApp/*.csproj ./src/PongRank.ConsoleApp/

RUN dotnet restore

COPY src/PongRank.Frenoy/. ./src/PongRank.Frenoy/
COPY src/PongRank.DataAccess/. ./src/PongRank.DataAccess/
COPY src/PongRank.DataEntities/. ./src/PongRank.DataEntities/
COPY src/PongRank.Model/. ./src/PongRank.Model/
COPY src/PongRank.ML/. ./src/PongRank.ML/
COPY src/PongRank.WebApi/. ./src/PongRank.WebApi/
COPY src/PongRank.ConsoleApp/. ./src/PongRank.ConsoleApp/

WORKDIR /source/src/PongRank.WebApi
RUN dotnet publish -c Release -o /app



FROM mcr.microsoft.com/dotnet/aspnet:8.0 as runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT [ "dotnet", "/app/PongRank.WebApi.dll" ]
