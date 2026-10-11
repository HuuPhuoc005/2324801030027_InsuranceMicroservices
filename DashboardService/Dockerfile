FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DashboardService.Api/ DashboardService.Api/
COPY DashboardService/ DashboardService/
RUN dotnet publish DashboardService/DashboardService.csproj -c Release -o /out
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:5035
ENV Urls=http://+:5035
ENV eureka__client__serviceUrl=http://eureka-server:8761/eureka/
ENV eureka__instance__hostName=dotnet-dashboard-service
ENTRYPOINT ["dotnet", "DashboardService.dll"]
