FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Oravity.Core/Oravity.Core.csproj", "src/Oravity.Core/"]
COPY ["src/Oravity.SharedKernel/Oravity.SharedKernel.csproj", "src/Oravity.SharedKernel/"]
COPY ["src/Oravity.Infrastructure/Oravity.Infrastructure.csproj", "src/Oravity.Infrastructure/"]

RUN dotnet restore "src/Oravity.Core/Oravity.Core.csproj"

COPY . .
WORKDIR "/src/src/Oravity.Core"
RUN dotnet build "Oravity.Core.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Oravity.Core.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Oravity.Core.dll"]
