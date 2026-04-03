#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_DIR="$SCRIPT_DIR/infra"

# Check prerequisites
for cmd in az terraform; do
  if ! command -v "$cmd" &>/dev/null; then
    echo "Error: $cmd is not installed." >&2
    exit 1
  fi
done

# Ensure logged in to Azure
if ! az account show &>/dev/null; then
  echo "Not logged in to Azure. Running 'az login'..."
  az login
fi

cd "$INFRA_DIR"

TF_LOG=INFO terraform init -upgrade
TF_LOG=INFO terraform apply

echo ""
echo "Next: run ./setup-github.sh <GHCR_PAT>"
