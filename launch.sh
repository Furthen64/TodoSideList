#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp}"
export HOME="${HOME:-/tmp}"

CONFIGURATION="Debug"

if [[ "${1:-}" == "Debug" || "${1:-}" == "Release" ]]; then
  CONFIGURATION="$1"
  shift
fi

cd "$ROOT_DIR"
dotnet run --project TodoSideList.App/TodoSideList.App.csproj -c "$CONFIGURATION" -- "$@"
