resource "azurerm_resource_group" "main" {
  name     = "rg-${var.project}"
  location = var.location
}

resource "azurerm_container_app_environment" "main" {
  name                = "cae-${var.project}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
}

resource "azurerm_container_app" "api" {
  name                         = "ca-${var.project}"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "api"
      image  = "mcr.microsoft.com/dotnet/samples:aspnetapp"
      cpu    = 0.25
      memory = "0.5Gi"
    }

    http_scale_rule {
      name                = "http-scaler"
      concurrent_requests = 10
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  # Image is managed by the deploy workflow, not Terraform
  lifecycle {
    ignore_changes = [
      template[0].container[0].image,
      secret,
      registry,
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
