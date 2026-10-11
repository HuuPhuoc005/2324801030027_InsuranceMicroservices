FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY PolicyService.Api/ PolicyService.Api/
COPY PolicyService/ PolicyService/
RUN dotnet publish PolicyService/PolicyService.csproj -c Release -o /out
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:5050
ENV Urls=http://+:5050
ENV eureka__client__serviceUrl=http://eureka-server:8761/eureka/
ENV eureka__instance__hostName=dotnet-policy-service
ENTRYPOINT ["dotnet", "PolicyService.dll"]
