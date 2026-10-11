FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AuthService.Api/ AuthService.Api/
COPY AuthService/ AuthService/
RUN dotnet publish AuthService/AuthService.csproj -c Release -o /out
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:6060
ENV Urls=http://+:6060
ENV eureka__client__serviceUrl=http://eureka-server:8761/eureka/
ENV eureka__instance__hostName=dotnet-auth-service
ENTRYPOINT ["dotnet", "AuthService.dll"]
