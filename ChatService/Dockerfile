FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ChatService.Api/ ChatService.Api/
COPY ChatService/ ChatService/
RUN dotnet publish ChatService/ChatService.csproj -c Release -o /out
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:4099
ENV Urls=http://+:4099
ENV eureka__client__serviceUrl=http://eureka-server:8761/eureka/
ENV eureka__instance__hostName=dotnet-chat-service
ENTRYPOINT ["dotnet", "ChatService.dll"]
