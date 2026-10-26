#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
WORKDIR /repo
COPY ["src/web/Notary.Web.csproj", "src/web/"]
COPY ["src/library/core/Notary.csproj", "src/library/core/"]
COPY ["src/library/service/Notary.Service.csproj", "src/library/service/"]
COPY ["src/library/data/Notary.Data.csproj", "src/library/data/"]
COPY ["Notary.ServiceDefaults/Notary.ServiceDefaults.csproj", "Notary.ServiceDefaults/"]
RUN dotnet restore "./src/web/Notary.Web.csproj" -a ${TARGETARCH}
COPY src/ src/
COPY Notary.ServiceDefaults/ Notary.ServiceDefaults/
WORKDIR "/repo/src/web"
# No --no-restore on build and publish: the restore above saw only the csproj files, and the SDK then leaves out
# Blazor's framework scripts (_framework/blazor.server.js), so the published image returned 404 for them.
RUN dotnet build "./Notary.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build --nologo -a ${TARGETARCH}

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
RUN dotnet publish "./Notary.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --nologo -a ${TARGETARCH}

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notary.Web.dll"]