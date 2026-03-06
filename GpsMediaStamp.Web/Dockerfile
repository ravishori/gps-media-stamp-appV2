# -----------------------------

# BUILD STAGE

# -----------------------------

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy entire solution

COPY . .

# Restore dependencies

RUN dotnet restore ./GpsMediaStamp.Web/GpsMediaStamp.Web.csproj

# Publish web project

RUN dotnet publish ./GpsMediaStamp.Web/GpsMediaStamp.Web.csproj 
-c Release 
-o /app/publish

# -----------------------------

# RUNTIME STAGE

# -----------------------------

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install FFmpeg (needed for video stamping)

RUN apt-get update && apt-get install -y ffmpeg && rm -rf /var/lib/apt/lists/*

# Copy compiled app

COPY --from=build /app/publish .

# Create folders for video storage

RUN mkdir -p /storage/raw /storage/stamped

# Cloud Run port

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GpsMediaStamp.Web.dll"]
