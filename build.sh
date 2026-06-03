#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp}"
export HOME="${HOME:-/tmp}"

CONFIGURATION="${1:-Debug}"

cd "$ROOT_DIR"
dotnet restore TodoSideList.slnx
dotnet build TodoSideList.slnx -c "$CONFIGURATION" --no-restore
