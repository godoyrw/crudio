FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copia projeto corretamente
COPY src/CrudIo.Api/CrudIo.Api.csproj CrudIo.Api/

RUN dotnet restore CrudIo.Api/CrudIo.Api.csproj

# copia tudo do projeto
COPY src/CrudIo.Api/ CrudIo.Api/

WORKDIR /src/CrudIo.Api

RUN dotnet publish CrudIo.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5051
EXPOSE 5051

ENTRYPOINT ["dotnet", "CrudIo.Api.dll"]