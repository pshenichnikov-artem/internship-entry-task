#!/bin/bash
set -e

echo "Waiting for PostgreSQL to be available..."
until pg_isready -h db -p 5432; do
  echo "PostgreSQL is unavailable - sleeping"
  sleep 2
done

echo "Starting application..."
exec dotnet "Tic Tac Toe.dll"
