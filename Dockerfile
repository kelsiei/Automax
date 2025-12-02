# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Automax.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish Automax.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
# TODO: Mount /app/data as a volume for persistent LiteDB/Postgres configuration in later phases.
ENTRYPOINT ["dotnet", "Automax.dll"]
