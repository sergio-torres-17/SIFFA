# Se construye desde la raíz del repositorio (ver docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src
COPY SIFFA.Core/SIFFA.Core.csproj SIFFA.Core/
COPY SIFFA.WebApp/SIFFA.WebApp.csproj SIFFA.WebApp/
RUN dotnet restore SIFFA.WebApp/SIFFA.WebApp.csproj
COPY SIFFA.Core/ SIFFA.Core/
COPY SIFFA.WebApp/ SIFFA.WebApp/
RUN dotnet publish SIFFA.WebApp/SIFFA.WebApp.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS final
# SkiaSharp (dibujo de las invitaciones) necesita fontconfig y una fuente.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 fonts-dejavu-core \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SIFFA.WebApp.dll"]
