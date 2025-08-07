FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# First error:
# System.DllNotFoundException: Unable to load shared library 'lib_lightgbm' or one of its dependencies. In order to help diagnose loading problems, consider using a tool like strace. If you're using glibc, consider setting the LD_DEBUG environment variable:
# libgomp.so.1: cannot open shared object file: No such file or directory
# /app/lib_lightgbm.so: cannot open shared object file: No such file or directory

# Second error:
# CMake 3.28 or higher is required.  You are running version 3.25.1

RUN apt-get update
RUN apt-get install git clang-3.9 libunwind8 curl libomp-dev -y

RUN apt-get install python3-pip -y
RUN pip install cmake --upgrade --break-system-packages
#RUN cmake --version

RUN git clone --recursive https://github.com/microsoft/LightGBM.git
RUN cd LightGBM && mkdir build && cd build && cmake .. && make -j && make install
#RUN find / -name 'lib_lightgbm.so' 2>/dev/null

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



FROM mcr.microsoft.com/dotnet/aspnet:9.0 as runtime


WORKDIR /app
COPY --from=build /app .

COPY --from=build /usr/local/lib/lib_lightgbm.so /usr/local/lib/lib_lightgbm.so
COPY --from=build /usr/lib/x86_64-linux-gnu/libgomp.so.1 /usr/lib/x86_64-linux-gnu/libgomp.so.1

ENTRYPOINT [ "dotnet", "/app/PongRank.WebApi.dll" ]
