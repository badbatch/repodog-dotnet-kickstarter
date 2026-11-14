#!/usr/bin/env bash

set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

dotnet tool restore

coverage_run_dir="tests/Kickstarter.IntegrationTests/TestResults/run-$(date +%Y%m%d%H%M%S)-$$"
dotnet test Kickstarter.sln \
    --settings Coverage.runsettings \
    --collect:"XPlat Code Coverage" \
    --results-directory "$coverage_run_dir"

dotnet tool run reportgenerator -- \
    -reports:"$coverage_run_dir/**/coverage.cobertura.xml" \
    -targetdir:"coverage-report" \
    -reporttypes:Html

if command -v open >/dev/null 2>&1; then
    open coverage-report/index.html
else
    printf 'Coverage report generated at %s/coverage-report/index.html\n' "$PWD"
fi
