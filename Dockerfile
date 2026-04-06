FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["OrgTechRepair/OrgTechRepair.csproj", "OrgTechRepair/"]
RUN dotnet restore "OrgTechRepair/OrgTechRepair.csproj"

COPY . .
WORKDIR /src/OrgTechRepair
RUN dotnet publish "OrgTechRepair.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 10000

ENTRYPOINT ["dotnet", "OrgTechRepair.dll"]
