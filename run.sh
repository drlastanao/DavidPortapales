#!/bin/bash
# Script para ejecutar la aplicación DavidPortapales localmente

# Cambiar al directorio donde se encuentra el script
cd "$(dirname "$0")" || exit

echo "🚀 Iniciando proceso..."

echo "🧪 Ejecutando pruebas..."
dotnet test DavidPortapales.slnx

if [ $? -ne 0 ]; then
    echo "❌ Las pruebas fallaron. Cancelando ejecución."
    exit 1
fi

echo "🧹 Limpiando proyecto..."
dotnet clean

echo "🔨 Compilando aplicación..."
dotnet build DavidPortapales.csproj

if [ $? -ne 0 ]; then
    echo "❌ Error de compilación."
    exit 1
fi

echo "🚀 Iniciando DavidPortapales..."
dotnet run --no-build --project DavidPortapales.csproj
