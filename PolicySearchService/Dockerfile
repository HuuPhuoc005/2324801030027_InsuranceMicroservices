FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY PolicySearchService.Api/ PolicySearchService.Api/
COPY PolicySearchService/ PolicySearchService/
RUN dotnet publish PolicySearchService/PolicySearchService.csproj -c Release -o /out
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:5065
ENV Urls=http://+:5065
ENV eureka__client__serviceUrl=http://eureka-server:8761/eureka/
ENV eureka__instance__hostName=dotnet-policysearch-service
ENTRYPOINT ["dotnet", "PolicySearchService.dll"]
