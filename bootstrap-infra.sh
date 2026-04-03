#!/usr/bin/env bash
#
# One-time setup: creates an Azure Storage Account to hold Terraform state.
# Run this before deploy-infra.sh.
#
# Usage:
#   ./bootstrap-infra.sh <SUBSCRIPTION_ID> [LOCATION]
#
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <SUBSCRIPTION_ID> [LOCATION]"
  exit 1
fi

SUBSCRIPTION_ID="$1"
LOCATION="${2:-uksouth}"
PROJECT="numchen"
RG_NAME="rg-${PROJECT}-tfstate"
STORAGE_ACCOUNT="st${PROJECT}tfstate"
CONTAINER_NAME="tfstate"

for cmd in az; do
  if ! command -v "$cmd" &>/dev/null; then
    echo "Error: $cmd is not installed." >&2
    exit 1
  fi
done

if ! az account show &>/dev/null; then
  echo "Not logged in to Azure. Running 'az login'..."
  az login
fi

az account set --subscription "$SUBSCRIPTION_ID"

echo "Creating resource group '$RG_NAME'..."
az group create \
  --name "$RG_NAME" \
  --location "$LOCATION" \
  --output none

echo "Creating storage account '$STORAGE_ACCOUNT'..."
az storage account create \
  --name "$STORAGE_ACCOUNT" \
  --resource-group "$RG_NAME" \
  --location "$LOCATION" \
  --sku Standard_LRS \
  --min-tls-version TLS1_2 \
  --output none

echo "Creating blob container '$CONTAINER_NAME'..."
az storage container create \
  --name "$CONTAINER_NAME" \
  --account-name "$STORAGE_ACCOUNT" \
  --auth-mode login \
  --output none

echo ""
echo "Terraform state backend is ready."
echo ""
echo "  Resource group:  $RG_NAME"
echo "  Storage account: $STORAGE_ACCOUNT"
echo "  Container:       $CONTAINER_NAME"
echo ""
echo "Next: run ./deploy-infra.sh"
