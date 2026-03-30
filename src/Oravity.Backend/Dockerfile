FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Oravity.Backend/Oravity.Backend.csproj", "src/Oravity.Backend/"]
COPY ["src/Oravity.SharedKernel/Oravity.SharedKernel.csproj", "src/Oravity.SharedKernel/"]
COPY ["src/Oravity.Infrastructure/Oravity.Infrastructure.csproj", "src/Oravity.Infrastructure/"]

RUN dotnet restore "src/Oravity.Backend/Oravity.Backend.csproj"

COPY . .
WORKDIR "/src/src/Oravity.Backend"
RUN dotnet build "Oravity.Backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Oravity.Backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Oravity.Backend.dll"]
