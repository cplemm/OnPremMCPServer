# build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY mcpserver.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /out

# run
FROM mcr.microsoft.com/dotnet/aspnet:9.0
EXPOSE 8080
EXPOSE 8081
COPY --from=build /out .
ENTRYPOINT ["dotnet", "mcpserver.dll"]
