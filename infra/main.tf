resource "azurerm_resource_group" "main" {
  name     = "rg-${var.project}"
  location = var.location
}

resource "azurerm_service_plan" "api" {
  name                = "asp-${var.project}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  os_type             = "Linux"
  sku_name            = "F1"
}

resource "azurerm_linux_web_app" "api" {
  name                = "app-${var.project}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  service_plan_id     = azurerm_service_plan.api.id

  https_only = true

  site_config {
    always_on         = false
    use_32_bit_worker = true
    ftps_state        = "Disabled"
    http2_enabled     = true

    application_stack {
      dotnet_version = "10.0"
    }
  }

  app_settings = {
    "WEBSITE_RUN_FROM_PACKAGE" = "1"
    "ASPNETCORE_ENVIRONMENT"   = "Production"
  }

  # App settings and deployed package are managed by the deploy workflow
  lifecycle {
    ignore_changes = [
      app_settings["AllowedOrigins"],
    ]
  }
}

resource "azurerm_static_web_app" "ui" {
  name                = "stapp-${var.project}"
  resource_group_name = azurerm_resource_group.main.name
  location            = "westeurope"
  sku_tier            = "Free"
  sku_size            = "Free"
}

# Service principal for GitHub Actions OIDC authentication
data "azurerm_client_config" "current" {}

resource "azurerm_user_assigned_identity" "deploy" {
  name                = "id-${var.project}-deploy"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
}

resource "azurerm_role_assignment" "deploy_contributor" {
  scope                = azurerm_resource_group.main.id
  role_definition_name = "Contributor"
  principal_id         = azurerm_user_assigned_identity.deploy.principal_id
}

resource "time_sleep" "wait_for_identity" {
  depends_on      = [azurerm_user_assigned_identity.deploy]
  create_duration = "30s"
}

resource "azurerm_federated_identity_credential" "github_actions" {
  name                = "github-actions-main"
  resource_group_name = azurerm_resource_group.main.name
  parent_id           = azurerm_user_assigned_identity.deploy.id
  depends_on          = [time_sleep.wait_for_identity]
  audience            = ["api://AzureADTokenExchange"]
  issuer              = "https://token.actions.githubusercontent.com"
  subject             = "repo:${var.github_repo}:ref:refs/heads/main"
}
