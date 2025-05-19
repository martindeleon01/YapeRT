# STAGE 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar los archivos de proyecto necesarios
COPY Common/Common.csproj Common/
COPY Transactions/Transactions.csproj Transactions/

# Restaurar dependencias
RUN dotnet restore Transactions/Transactions.csproj

# Copiar solo el código fuente necesario
COPY Common/ Common/
COPY Transactions/ Transactions/

# Compilar el proyecto
WORKDIR /src/Transactions
RUN dotnet build -c Release -o /app/build

# STAGE 2: Publish y generar imagen final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/build .

ENTRYPOINT ["dotnet", "Transactions.dll"]