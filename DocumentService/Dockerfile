FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DocumentService.Api/ DocumentService.Api/
COPY DocumentService/ DocumentService/
RUN dotnet publish DocumentService/DocumentService.csproj -c Release -o /out
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:5080
ENV Urls=http://+:5080
ENV eureka__client__serviceUrl=http://eureka-server:8761/eureka/
ENV eureka__instance__hostName=dotnet-document-service
ENTRYPOINT ["dotnet", "DocumentService.dll"]
