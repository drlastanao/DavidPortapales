#!/bin/bash
# Script para ejecutar la aplicación DavidPortapales localmente

# Cambiar al directorio donde se encuentra el script
cd "$(dirname "$0")" || exit

echo "🚀 Iniciando DavidPortapales..."
dotnet run --no-build --project DavidPortapales.csproj
