FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ProductService.Api/ ProductService.Api/
COPY ProductService/ ProductService/
RUN dotnet publish ProductService/ProductService.csproj -c Release -o /out
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:5030
ENV Urls=http://+:5030
ENV eureka__client__serviceUrl=http://eureka-server:8761/eureka/
ENV eureka__instance__hostName=dotnet-product-service
ENTRYPOINT ["dotnet", "ProductService.dll"]
