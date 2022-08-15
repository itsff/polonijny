
#
# Usage:
# docker build . -f Dockerfile -t polonijny:latest
# docker run -it -e Mongo__ConnectionString="mongodb+srv://..." -e Mongo__DatabaseName="slownik_polonijny" -e Mongo__CollectionName=entries -p 5000:80 polonijny:latest
# docker save polonijny:latest > polonijny.tar
# docker load -i polonijny.tar -t polonijny:latest
#

#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim-amd64 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim-amd64 AS build
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