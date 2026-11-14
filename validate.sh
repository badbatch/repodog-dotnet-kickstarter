#!/usr/bin/env bash

set -euo pipefail

SOLUTION="Kickstarter.sln"

echo "==> Checking CSharpier formatting…"
dotnet csharpier check .

echo "==> Running Roslynator analysis…"
dotnet tool run roslynator -- analyze "$SOLUTION"

echo "==> Running tests…"
dotnet test "$SOLUTION" --no-restore --logger "console;verbosity=normal"

echo "==> All checks passed!"
