FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore ./GpsMediaStamp.Web/GpsMediaStamp.Web.csproj

RUN dotnet publish ./GpsMediaStamp.Web/GpsMediaStamp.Web.csproj \
-c Release \
-o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update && apt-get install -y ffmpeg && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN mkdir -p /storage/raw /storage/stamped

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV PORT=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "GpsMediaStamp.Web.dll"]