#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
WORKDIR /src
COPY ["./web/Notary.Web.csproj", "web/"]
COPY ["./library/core/Notary.csproj", "library/core/"]
COPY ["./library/service/Notary.Service.csproj", "library/service/"]
COPY ["./library/data/Notary.Data.csproj", "library/data/"]
RUN dotnet restore "./web/Notary.Web.csproj" -a ${TARGETARCH}
COPY . .
WORKDIR "/src/web"
RUN dotnet build "./Notary.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build --nologo --no-restore -a ${TARGETARCH}

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
RUN dotnet publish "./Notary.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --nologo --no-restore -a ${TARGETARCH}

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notary.Web.dll"]