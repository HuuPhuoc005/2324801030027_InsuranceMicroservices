FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY PaymentService.Api/ PaymentService.Api/
COPY PaymentService/ PaymentService/
RUN dotnet publish PaymentService/PaymentService.csproj -c Release -o /out
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:5100
ENV Urls=http://+:5100
ENV eureka__client__serviceUrl=http://eureka-server:8761/eureka/
ENV eureka__instance__hostName=dotnet-payment-service
ENTRYPOINT ["dotnet", "PaymentService.dll"]
