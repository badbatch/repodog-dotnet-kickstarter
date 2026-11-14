FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Kickstarter.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

COPY ["src/Kickstarter.Api/Kickstarter.Api.csproj", "src/Kickstarter.Api/"]
COPY ["src/Kickstarter.Application/Kickstarter.Application.csproj", "src/Kickstarter.Application/"]
COPY ["src/Kickstarter.Domain/Kickstarter.Domain.csproj", "src/Kickstarter.Domain/"]
COPY ["src/Kickstarter.Infrastructure/Kickstarter.Infrastructure.csproj", "src/Kickstarter.Infrastructure/"]
COPY ["tests/Kickstarter.IntegrationTests/Kickstarter.IntegrationTests.csproj", "tests/Kickstarter.IntegrationTests/"]
COPY ["tests/Kickstarter.TestCommon/Kickstarter.TestCommon.csproj", "tests/Kickstarter.TestCommon/"]

RUN dotnet restore "Kickstarter.sln"

COPY . .
RUN dotnet publish "src/Kickstarter.Api/Kickstarter.Api.csproj" --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Kickstarter.Api.dll"]
