FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BankingSystem.API/BankingSystem.API.csproj BankingSystem.API/
COPY BankingSystem.Business/BankingSystem.Business.csproj BankingSystem.Business/
COPY BankingSystem.Core/BankingSystem.Core.csproj BankingSystem.Core/
COPY BankingSystem.Data/BankingSystem.Data.csproj BankingSystem.Data/
RUN dotnet restore BankingSystem.API/BankingSystem.API.csproj

COPY . .
RUN dotnet publish BankingSystem.API/BankingSystem.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "BankingSystem.API.dll"]