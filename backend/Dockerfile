FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src
COPY AuraWellness.slnx .
COPY src/ src/
RUN dotnet restore
RUN dotnet publish src/AuraWellness.API/AuraWellness.API.csproj \
    -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AuraWellness.API.dll"]
