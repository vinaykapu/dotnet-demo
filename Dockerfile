FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY DotnetApp.sln ./
COPY src/DotnetApp/DotnetApp.csproj ./src/DotnetApp/
COPY tests/DotnetApp.Tests/DotnetApp.Tests.csproj ./tests/DotnetApp.Tests/
RUN dotnet restore

COPY . .
RUN dotnet test tests/DotnetApp.Tests/DotnetApp.Tests.csproj --no-restore --verbosity minimal
RUN dotnet publish src/DotnetApp/DotnetApp.csproj -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DotnetApp.dll"]
