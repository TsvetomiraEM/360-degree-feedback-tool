FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Feedback360.Domain/Feedback360.Domain.csproj src/Feedback360.Domain/
COPY src/Feedback360.Application/Feedback360.Application.csproj src/Feedback360.Application/
COPY src/Feedback360.Infrastructure/Feedback360.Infrastructure.csproj src/Feedback360.Infrastructure/
COPY src/Feedback360.Api/Feedback360.Api.csproj src/Feedback360.Api/
RUN dotnet restore src/Feedback360.Api/Feedback360.Api.csproj
COPY src/ src/
RUN dotnet publish src/Feedback360.Api/Feedback360.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Feedback360.Api.dll"]
