#!/usr/bin/env bash
#
# Configures GitHub repository secrets and variables from Terraform outputs.
# Also sets up ghcr.io registry credentials on the Container App.
#
# Prerequisites:
#   - Terraform has been applied (run deploy-infra.sh first)
#   - gh CLI is authenticated
#   - az CLI is authenticated
#   - A GitHub PAT with read:packages scope (for Container App to pull images)
#
# Usage:
#   ./setup-github.sh <GHCR_PAT>
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_DIR="$SCRIPT_DIR/infra"

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <GHCR_PAT>"
  echo ""
  echo "GHCR_PAT: A GitHub Personal Access Token (classic) with read:packages scope."
  echo "          Create one at https://github.com/settings/tokens/new?scopes=read:packages"
  exit 1
fi

GHCR_PAT="$1"

for cmd in az gh terraform; do
  if ! command -v "$cmd" &>/dev/null; then
    echo "Error: $cmd is not installed." >&2
    exit 1
  fi
done

cd "$INFRA_DIR"

echo "Reading Terraform outputs..."
RG_NAME=$(terraform output -raw resource_group_name)
CONTAINER_APP_NAME=$(terraform output -raw container_app_name)
API_FQDN=$(terraform output -raw api_fqdn)
SWA_NAME=$(terraform output -raw static_web_app_name)
CLIENT_ID=$(terraform output -raw deploy_client_id)
TENANT_ID=$(terraform output -raw tenant_id)
SUBSCRIPTION_ID=$(terraform output -raw subscription_id)

echo "Fetching Static Web App deployment token..."
SWA_TOKEN=$(az staticwebapp secrets list \
  --name "$SWA_NAME" \
  --resource-group "$RG_NAME" \
  --query "properties.apiKey" -o tsv)

echo "Configuring ghcr.io registry on Container App..."
GITHUB_USERNAME=$(gh api user --jq .login)
az containerapp registry set \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RG_NAME" \
  --server ghcr.io \
  --username "$GITHUB_USERNAME" \
  --password "$GHCR_PAT"

echo "Setting GitHub variables..."
gh variable set AZURE_CLIENT_ID --body "$CLIENT_ID"
gh variable set AZURE_TENANT_ID --body "$TENANT_ID"
gh variable set AZURE_SUBSCRIPTION_ID --body "$SUBSCRIPTION_ID"
gh variable set AZURE_RESOURCE_GROUP --body "$RG_NAME"
gh variable set AZURE_CONTAINER_APP_NAME --body "$CONTAINER_APP_NAME"
gh variable set VITE_API_URL --body "https://$API_FQDN"

echo "Setting GitHub secrets..."
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN --body "$SWA_TOKEN"

echo ""
echo "Done! GitHub repository is configured for deployment."
echo ""
echo "  API URL:  https://$API_FQDN"
echo "  UI URL:   https://$(terraform output -raw static_web_app_hostname)"
echo ""
echo "Push to main to trigger a deployment."
