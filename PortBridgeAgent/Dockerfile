FROM mcr.microsoft.com/dotnet/core/sdk:2.1 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY ./PortBridgeAgent ./PortBridgeAgent
COPY ./AzureRelayPortBridge ./AzureRelayPortBridge
RUN dotnet restore ./PortBridgeAgent/PortBridgeAgent.csproj

# copy and publish app and libraries
RUN dotnet publish ./PortBridgeAgent/PortBridgeAgent.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/core/runtime:2.1 AS runtime
WORKDIR /app
COPY --from=build /app/PortBridgeAgent/out ./
ENTRYPOINT ["dotnet", "PortBridgeAgent.dll"]