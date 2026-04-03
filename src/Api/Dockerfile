FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Api/Api.csproj src/Api/
RUN dotnet restore src/Api/Api.csproj
COPY src/Domain/ src/Domain/
COPY src/Api/ src/Api/
RUN dotnet publish src/Api -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]
