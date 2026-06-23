FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY TelephonyCallService.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/data
VOLUME ["/app/data"]
ENV Database__Path=/app/data/sessions.db
EXPOSE 8080
ENTRYPOINT ["dotnet", "TelephonyCallService.dll"]
