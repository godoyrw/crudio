#!/bin/bash
set -e
echo "🔨 Building..."
docker build -t crudio-api:local .

echo "📦 Loading into kind..."
kind load docker-image crudio-api:local --name crudio-local

echo "🔄 Restarting..."
kubectl rollout restart deployment/crudio-api -n crudio-local
kubectl rollout status deployment/crudio-api -n crudio-local

echo "✅ Done!"
