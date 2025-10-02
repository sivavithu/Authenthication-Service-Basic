FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OAuthAuth/OAuthAuth.csproj", "OAuthAuth/"]
RUN dotnet restore "OAuthAuth/OAuthAuth.csproj"
COPY . .
WORKDIR "/src/OAuthAuth"
RUN dotnet build "OAuthAuth.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OAuthAuth.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OAuthAuth.dll"]
```