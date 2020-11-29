#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim-amd64 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim-amd64 AS build
WORKDIR .
COPY ["SlownikPolonijny.Dal/", "SlownikPolonijny.Dal/"]
COPY ["SlownikPolonijny.Dal.Mongo/", "SlownikPolonijny.Dal.Mongo/"]
COPY ["SlownikPolonijny.Web/", "SlownikPolonijny.Web/"]
WORKDIR "SlownikPolonijny.Web/"
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SlownikPolonijny.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SlownikPolonijny.Web.dll"]