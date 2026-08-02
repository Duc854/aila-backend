FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["AILA.Api/AILA.Api.csproj", "AILA.Api/"]
COPY ["AILA.Application/AILA.Application.csproj", "AILA.Application/"]
COPY ["AILA.Domain/AILA.Domain.csproj", "AILA.Domain/"]
COPY ["AILA.Infrastructure/AILA.Infrastructure.csproj", "AILA.Infrastructure/"]
COPY ["Shared/Shared.csproj", "Shared/"]

RUN dotnet restore "AILA.Api/AILA.Api.csproj"

COPY . .

WORKDIR "/src/AILA.Api"

RUN dotnet publish "AILA.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "AILA.Api.dll"]