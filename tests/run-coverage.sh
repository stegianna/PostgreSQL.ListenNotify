#!/usr/bin/env bash

# run-coverage.sh

set -euo pipefail

# Paths
TESTS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEST_PROJECT="$TESTS_DIR/PostgreSQL.ListenNotify.Tests/PostgreSQL.ListenNotify.Tests.csproj"
OUTPUT_DIR="$TESTS_DIR/TestResults"
COVERAGE_FILE="$OUTPUT_DIR/coverage.cobertura.xml"
REPORT_DIR="$TESTS_DIR/CoverageReport"

# Make sure output folders exist
mkdir -p "$OUTPUT_DIR"
mkdir -p "$REPORT_DIR"

# Optional: Ensure ReportGenerator is installed
if ! command -v reportgenerator &> /dev/null; then
    echo "Installing ReportGenerator..."
    dotnet tool install --global dotnet-reportgenerator-globaltool
    export PATH="$PATH:$HOME/.dotnet/tools"
fi

# Run tests with Coverlet coverage enabled, excluding test projects
echo "Running tests and collecting coverage..."

dotnet test "$TEST_PROJECT" \
    /p:CollectCoverage=true \
    /p:CoverletOutput="$OUTPUT_DIR/coverage" \
    /p:CoverletOutputFormat=cobertura \
    /p:Exclude="[*.Tests*]*"

# Generate HTML report
echo "Generating HTML report..."

export DOTNET_ROOT="$(ls -d /opt/homebrew/Cellar/dotnet/*/libexec | tail -1)"

reportgenerator \
    -reports:"$COVERAGE_FILE" \
    -targetdir:"$REPORT_DIR" \
    -reporttypes:Html

echo "Coverage report generated at: $REPORT_DIR/index.html"